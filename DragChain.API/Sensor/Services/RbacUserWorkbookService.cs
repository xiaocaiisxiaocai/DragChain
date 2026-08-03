using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Services;

public sealed record RbacUserImportRow(
    int RowNumber,
    string EmployeeNo,
    string Name,
    string Role,
    bool Enabled,
    string Password);

public sealed class RbacUserWorkbookParseResult
{
    public List<RbacUserImportRow> Rows { get; } = [];
    public List<string> Errors { get; } = [];
}

public static partial class RbacUserWorkbookService
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private static readonly string[] ExportHeaders = ["工号", "姓名", "角色", "启用", "密码"];

    public static string ExcelContentType => ContentType;

    public static byte[] CreateWorkbook(IEnumerable<RbacUser> users)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypesXml());
            WriteEntry(archive, "_rels/.rels", RootRelsXml());
            WriteEntry(archive, "xl/workbook.xml", WorkbookXml());
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelsXml());
            WriteEntry(archive, "xl/styles.xml", StylesXml());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(users));
        }

        return stream.ToArray();
    }

    public static RbacUserWorkbookParseResult ParseWorkbook(Stream stream)
    {
        var result = new RbacUserWorkbookParseResult();

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
            result.Errors.Add("文件中没有可导入的用户数据");
            return result;
        }

        var headerMap = BuildHeaderMap(rows[0]);
        for (var i = 1; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var row = rows[i];
            var employeeNo = GetCell(row, headerMap, "工号", "employeeNo", "username");
            var name = GetCell(row, headerMap, "姓名", "name");
            var role = GetCell(row, headerMap, "角色", "role");
            var enabledText = GetCell(row, headerMap, "启用", "状态", "enabled");
            var password = GetCell(row, headerMap, "密码", "password");

            if (string.IsNullOrWhiteSpace(employeeNo) &&
                string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(role) &&
                string.IsNullOrWhiteSpace(enabledText) &&
                string.IsNullOrWhiteSpace(password))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(employeeNo))
                result.Errors.Add($"第 {rowNumber} 行缺少工号");
            if (string.IsNullOrWhiteSpace(name))
                result.Errors.Add($"第 {rowNumber} 行缺少姓名");

            var normalizedRole = NormalizeRole(role);
            if (!string.IsNullOrWhiteSpace(role) && normalizedRole == null)
                result.Errors.Add($"第 {rowNumber} 行角色只能是 超级管理员/管理员/编辑/普通用户 或 super_admin/admin/editor/user");

            if (!TryParseEnabled(enabledText, out var enabled))
                result.Errors.Add($"第 {rowNumber} 行启用只能填写 是/否、启用/停用、true/false 或 1/0");

            result.Rows.Add(new RbacUserImportRow(
                rowNumber,
                employeeNo.Trim(),
                name.Trim(),
                normalizedRole ?? "user",
                enabled,
                password.Trim()));
        }

        if (result.Rows.Count == 0 && result.Errors.Count == 0)
            result.Errors.Add("文件中没有可导入的用户数据");

        return result;
    }

    private static string WorksheetXml(IEnumerable<RbacUser> users)
    {
        var rows = new StringBuilder();
        rows.Append(RowXml(1, ExportHeaders, isHeader: true));

        var rowNumber = 2;
        foreach (var user in users)
        {
            rows.Append(RowXml(rowNumber++,
            [
                user.EmployeeNo,
                user.Name,
                RoleLabel(user.Role),
                user.Enabled ? "启用" : "停用",
                ""
            ]));
        }

        return $$"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <dimension ref="A1:E{{Math.Max(rowNumber - 1, 1)}}"/>
  <sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>
  <cols>
    <col min="1" max="2" width="18" customWidth="1"/>
    <col min="3" max="5" width="16" customWidth="1"/>
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

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
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
        if (type == "s" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex) &&
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

    private static string? NormalizeRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "" or null => "user",
            "super_admin" or "超级管理员" => "super_admin",
            "admin" or "管理员" => "admin",
            "editor" or "编辑" => "editor",
            "user" or "普通用户" => "user",
            _ => null
        };
    }

    private static string RoleLabel(string role) => role switch
    {
        "super_admin" => "超级管理员",
        "admin" => "管理员",
        "editor" => "编辑",
        _ => "普通用户"
    };

    private static bool TryParseEnabled(string? value, out bool enabled)
    {
        enabled = true;
        if (string.IsNullOrWhiteSpace(value)) return true;

        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "yes":
            case "y":
            case "是":
            case "启用":
                enabled = true;
                return true;
            case "0":
            case "false":
            case "no":
            case "n":
            case "否":
            case "停用":
            case "禁用":
                enabled = false;
                return true;
            default:
                return false;
        }
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
  <sheets><sheet name="用户清单" sheetId="1" r:id="rId1"/></sheets>
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
