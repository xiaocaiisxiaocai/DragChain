using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Services;

public sealed record ProductImportRow(
    int RowNumber,
    int? Id,
    string Code,
    string Model,
    string Name,
    string Brand,
    string Type,
    string Spec,
    string Scene);

public sealed class ProductWorkbookParseResult
{
    public List<ProductImportRow> Rows { get; } = [];
    public List<string> Errors { get; } = [];
}

public static partial class ProductWorkbookService
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] ExportHeaders =
    [
        "ID", "料号", "型号", "名称", "品牌", "类型", "规格", "场景"
    ];

    public static string ExcelContentType => ContentType;

    public static byte[] CreateWorkbook(IEnumerable<Product> products, Func<string, string> getTypeName)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypesXml());
            WriteEntry(archive, "_rels/.rels", RootRelsXml());
            WriteEntry(archive, "xl/workbook.xml", WorkbookXml());
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelsXml());
            WriteEntry(archive, "xl/styles.xml", StylesXml());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(products, getTypeName));
        }

        return stream.ToArray();
    }

    public static ProductWorkbookParseResult ParseWorkbook(Stream stream)
    {
        var result = new ProductWorkbookParseResult();

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null)
        {
            result.Errors.Add("未找到第一个工作表");
            return result;
        }

        var sharedStrings = ReadSharedStrings(archive);
        using var sheetStream = sheetEntry.Open();
        var doc = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var rows = doc.Descendants(ns + "row")
            .Select(row => ReadRow(row, sharedStrings, ns))
            .Where(row => row.Count > 0)
            .ToList();

        if (rows.Count <= 1)
        {
            result.Errors.Add("文件中没有可导入的产品数据");
            return result;
        }

        var headerMap = BuildHeaderMap(rows[0]);
        for (var i = 1; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var row = rows[i];
            var code = GetCell(row, headerMap, "料号", "code");
            var model = GetCell(row, headerMap, "型号", "model");
            var name = GetCell(row, headerMap, "名称", "name");
            var type = GetCell(row, headerMap, "类型", "类型名称", "type");

            if (string.IsNullOrWhiteSpace(code) &&
                string.IsNullOrWhiteSpace(model) &&
                string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(code))
                result.Errors.Add($"第 {rowNumber} 行缺少料号");
            if (string.IsNullOrWhiteSpace(model))
                result.Errors.Add($"第 {rowNumber} 行缺少型号");
            if (string.IsNullOrWhiteSpace(name))
                result.Errors.Add($"第 {rowNumber} 行缺少名称");
            if (string.IsNullOrWhiteSpace(type))
                result.Errors.Add($"第 {rowNumber} 行缺少类型");

            var idText = GetCell(row, headerMap, "ID", "id");
            int? id = null;
            if (!string.IsNullOrWhiteSpace(idText))
            {
                if (int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
                    id = parsedId;
                else
                    result.Errors.Add($"第 {rowNumber} 行 ID 必须是数字");
            }

            result.Rows.Add(new ProductImportRow(
                rowNumber,
                id,
                code,
                model,
                name,
                GetCell(row, headerMap, "品牌", "brand"),
                type,
                GetCell(row, headerMap, "规格", "spec"),
                GetCell(row, headerMap, "场景", "scene")));
        }

        if (result.Rows.Count == 0 && result.Errors.Count == 0)
            result.Errors.Add("文件中没有可导入的产品数据");

        return result;
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string WorksheetXml(IEnumerable<Product> products, Func<string, string> getTypeName)
    {
        var rows = new StringBuilder();
        rows.Append(RowXml(1, ExportHeaders, isHeader: true));

        var rowNumber = 2;
        foreach (var product in products)
        {
            rows.Append(RowXml(rowNumber++,
            [
                product.Id.ToString(CultureInfo.InvariantCulture),
                product.Code ?? "",
                product.Model,
                product.Name,
                product.Brand,
                getTypeName(product.Type),
                product.Spec ?? "",
                product.Scene ?? ""
            ]));
        }

        return $$"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <dimension ref="A1:H{{Math.Max(rowNumber - 1, 1)}}"/>
  <sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>
  <cols>
    <col min="1" max="1" width="8" customWidth="1"/>
    <col min="2" max="2" width="16" customWidth="1"/>
    <col min="3" max="4" width="24" customWidth="1"/>
    <col min="5" max="6" width="18" customWidth="1"/>
    <col min="7" max="8" width="36" customWidth="1"/>
  </cols>
  <sheetData>{{rows}}</sheetData>
</worksheet>
""";
    }

    private static string RowXml(int rowNumber, IReadOnlyList<string> values, bool isHeader = false)
    {
        var cells = new StringBuilder();
        for (var i = 0; i < values.Count; i++)
        {
            var reference = $"{ColumnName(i + 1)}{rowNumber}";
            var style = isHeader ? " s=\"1\"" : "";
            cells.Append($"""<c r="{reference}" t="inlineStr"{style}><is><t>{SecurityElement.Escape(values[i] ?? "")}</t></is></c>""");
        }

        return $"""<row r="{rowNumber}">{cells}</row>""";
    }

    private static string ColumnName(int index)
    {
        var name = "";
        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }

        return name;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return [];

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return doc.Descendants(ns + "si")
            .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
            .ToList();
    }

    private static Dictionary<int, string> ReadRow(XElement row, IReadOnlyList<string> sharedStrings, XNamespace ns)
    {
        var values = new Dictionary<int, string>();
        foreach (var cell in row.Elements(ns + "c"))
        {
            var reference = cell.Attribute("r")?.Value ?? "";
            var columnIndex = CellColumnIndex(reference);
            if (columnIndex <= 0) continue;
            values[columnIndex] = ReadCellValue(cell, sharedStrings, ns).Trim();
        }

        return values;
    }

    private static int CellColumnIndex(string reference)
    {
        var match = CellReferenceRegex().Match(reference);
        if (!match.Success) return 0;

        var index = 0;
        foreach (var ch in match.Groups[1].Value.ToUpperInvariant())
        {
            index = index * 26 + ch - 'A' + 1;
        }

        return index;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings, XNamespace ns)
    {
        var type = cell.Attribute("t")?.Value;
        if (type == "inlineStr")
            return cell.Element(ns + "is")?.Element(ns + "t")?.Value ?? "";

        var value = cell.Element(ns + "v")?.Value ?? "";
        if (type == "s" && int.TryParse(value, out var sharedIndex) &&
            sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedIndex];
        }

        return value;
    }

    private static Dictionary<string, int> BuildHeaderMap(Dictionary<int, string> headerRow)
    {
        return headerRow
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .GroupBy(item => NormalizeHeader(item.Value))
            .ToDictionary(group => group.Key, group => group.First().Key);
    }

    private static string GetCell(Dictionary<int, string> row, Dictionary<string, int> headerMap, params string[] names)
    {
        foreach (var name in names)
        {
            if (headerMap.TryGetValue(NormalizeHeader(name), out var columnIndex) &&
                row.TryGetValue(columnIndex, out var value))
            {
                return value.Trim();
            }
        }

        return "";
    }

    private static string NormalizeHeader(string value) =>
        value.Trim().Replace(" ", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();

    private static string ContentTypesXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
</Types>
""";

    private static string RootRelsXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""";

    private static string WorkbookXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets><sheet name="产品清单" sheetId="1" r:id="rId1"/></sheets>
</workbook>
""";

    private static string WorkbookRelsXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
""";

    private static string StylesXml() => """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
  <fills count="1"><fill><patternFill patternType="none"/></fill></fills>
  <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
  <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>
</styleSheet>
""";

    [GeneratedRegex("^([A-Z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CellReferenceRegex();
}
