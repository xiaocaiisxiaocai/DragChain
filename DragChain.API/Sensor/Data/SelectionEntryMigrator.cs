using Microsoft.EntityFrameworkCore;
using System.Data;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Data;

public static class SelectionEntryMigrator
{
    public static async Task MigrateAsync(SensorDbContext db)
    {
        await CreateFrameworkTablesAsync(db);
        await SeedFrameworkDataAsync(db);
    }

    private static async Task CreateFrameworkTablesAsync(SensorDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "SelectionEntries" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SelectionEntries" PRIMARY KEY AUTOINCREMENT,
                "Code" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "Icon" TEXT NOT NULL DEFAULT '',
                "Description" TEXT NULL,
                "IsSystem" INTEGER NOT NULL DEFAULT 0,
                "SortOrder" INTEGER NOT NULL DEFAULT 0
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "BusinessNodes" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_BusinessNodes" PRIMARY KEY AUTOINCREMENT,
                "Code" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "NodeType" TEXT NOT NULL,
                "Icon" TEXT NOT NULL DEFAULT '',
                "Description" TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "EntryTreeNodes" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_EntryTreeNodes" PRIMARY KEY AUTOINCREMENT,
                "EntryId" INTEGER NOT NULL,
                "BusinessNodeId" INTEGER NOT NULL,
                "ParentId" INTEGER NULL,
                "DisplayName" TEXT NULL,
                "DescriptionOverride" TEXT NULL,
                "SortOrder" INTEGER NOT NULL DEFAULT 0,
                "InheritRules" INTEGER NOT NULL DEFAULT 1,
                CONSTRAINT "FK_EntryTreeNodes_SelectionEntries_EntryId" FOREIGN KEY ("EntryId") REFERENCES "SelectionEntries" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_EntryTreeNodes_BusinessNodes_BusinessNodeId" FOREIGN KEY ("BusinessNodeId") REFERENCES "BusinessNodes" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_EntryTreeNodes_EntryTreeNodes_ParentId" FOREIGN KEY ("ParentId") REFERENCES "EntryTreeNodes" ("Id") ON DELETE RESTRICT
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "RuleEntryBindings" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_RuleEntryBindings" PRIMARY KEY AUTOINCREMENT,
                "RuleId" INTEGER NOT NULL,
                "EntryTreeNodeId" INTEGER NOT NULL,
                "IncludeDescendants" INTEGER NOT NULL DEFAULT 0,
                "SortOrder" INTEGER NOT NULL DEFAULT 0,
                "Note" TEXT NULL,
                CONSTRAINT "FK_RuleEntryBindings_SelectionRules_RuleId" FOREIGN KEY ("RuleId") REFERENCES "SelectionRules" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_RuleEntryBindings_EntryTreeNodes_EntryTreeNodeId" FOREIGN KEY ("EntryTreeNodeId") REFERENCES "EntryTreeNodes" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "SelectionResults" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SelectionResults" PRIMARY KEY AUTOINCREMENT,
                "EntryTreeNodeId" INTEGER NOT NULL,
                "Note" TEXT NULL,
                "SortOrder" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT "FK_SelectionResults_EntryTreeNodes_EntryTreeNodeId" FOREIGN KEY ("EntryTreeNodeId") REFERENCES "EntryTreeNodes" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "SelectionResultProducts" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_SelectionResultProducts" PRIMARY KEY AUTOINCREMENT,
                "SelectionResultId" INTEGER NOT NULL,
                "ProductId" INTEGER NOT NULL,
                "Quantity" INTEGER NOT NULL DEFAULT 1,
                CONSTRAINT "FK_SelectionResultProducts_SelectionResults_SelectionResultId" FOREIGN KEY ("SelectionResultId") REFERENCES "SelectionResults" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_SelectionResultProducts_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_SelectionEntries_Code" ON "SelectionEntries" ("Code");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_BusinessNodes_Code" ON "BusinessNodes" ("Code");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_EntryTreeNodes_EntryId" ON "EntryTreeNodes" ("EntryId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_EntryTreeNodes_ParentId" ON "EntryTreeNodes" ("ParentId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_EntryTreeNodes_BusinessNodeId" ON "EntryTreeNodes" ("BusinessNodeId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_RuleEntryBindings_RuleId_EntryTreeNodeId" ON "RuleEntryBindings" ("RuleId", "EntryTreeNodeId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_SelectionResults_EntryTreeNodeId" ON "SelectionResults" ("EntryTreeNodeId");""");
        await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_SelectionResultProducts_Result_Product" ON "SelectionResultProducts" ("SelectionResultId", "ProductId");""");
    }

    private static async Task SeedFrameworkDataAsync(SensorDbContext db)
    {
        var mechanismEntry = await EnsureEntryAsync(db, "mechanism", "机构选型", "🏗️", "按机构、功能、条件逐层选型", 1);
        var processEntry = await EnsureEntryAsync(db, "process", "制程选型", "⚡", "按制程入口查看受影响机构和规则", 2);
        await EnsureEntryAsync(db, "model", "机型选型", "🧩", "预留机型入口，可由用户后续扩展", 3);

        await db.SaveChangesAsync();
        await SeedMechanismEntryAsync(db, mechanismEntry.Id);
        await SeedProcessEntryAsync(db, processEntry.Id);
        await EnsureDefaultRuleProductsAsync(db);
        await ImportLegacyWorkflowToSelectorAsync(db);
    }

    private static async Task<SelectionEntry> EnsureEntryAsync(
        SensorDbContext db,
        string code,
        string name,
        string icon,
        string description,
        int sortOrder)
    {
        var entry = await db.SelectionEntries.FirstOrDefaultAsync(e => e.Code == code);
        if (entry != null) return entry;

        entry = new SelectionEntry
        {
            Code = code,
            Name = name,
            Icon = icon,
            Description = description,
            IsSystem = true,
            SortOrder = sortOrder
        };
        db.SelectionEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    private static async Task SeedMechanismEntryAsync(SensorDbContext db, int entryId)
    {
        var scenarios = await db.Scenarios
            .Include(s => s.Functions).ThenInclude(f => f.Conditions)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        foreach (var scenario in scenarios)
        {
            var scenarioNode = await EnsureBusinessNodeAsync(db, $"mechanism:{scenario.Code}", scenario.Name, "mechanism", scenario.Icon, scenario.Desc);
            var scenarioTreeNode = await EnsureTreeNodeAsync(db, entryId, scenarioNode.Id, null, null, scenario.Desc, scenario.SortOrder);

            foreach (var function in scenario.Functions.OrderBy(f => f.SortOrder))
            {
                var functionNode = await EnsureBusinessNodeAsync(db, $"function:{function.Code}", function.Name, "function", function.Icon, function.Note);
                var functionTreeNode = await EnsureTreeNodeAsync(db, entryId, functionNode.Id, scenarioTreeNode.Id, null, function.Note, function.SortOrder);

                foreach (var condition in function.Conditions.OrderBy(c => c.SortOrder))
                {
                    var conditionNode = await EnsureBusinessNodeAsync(db, $"condition:{condition.Code}", condition.Name, "condition", "", condition.Note);
                    var conditionTreeNode = await EnsureTreeNodeAsync(db, entryId, conditionNode.Id, functionTreeNode.Id, null, condition.Note, condition.SortOrder);

                    var rule = await db.SelectionRules.FirstOrDefaultAsync(r => r.ConditionId == condition.Id);
                    if (rule != null)
                        await EnsureBindingAsync(db, rule.Id, conditionTreeNode.Id, includeDescendants: false, rule.Id, "旧机构规则自动绑定");
                }
            }
        }
    }

    private static async Task SeedProcessEntryAsync(SensorDbContext db, int entryId)
    {
        var processScenarios = await db.ProcessScenarios
            .Include(ps => ps.AffectedMechanisms)
            .OrderBy(ps => ps.SortOrder)
            .ToListAsync();

        var conditionCodeToRule = await db.SelectionRules
            .Include(r => r.Condition)
            .Where(r => r.Condition != null)
            .ToDictionaryAsync(r => r.Condition!.Code, r => r.Id);

        foreach (var process in processScenarios)
        {
            var processNode = await EnsureBusinessNodeAsync(db, $"process:{process.Code}", process.Name, "process", process.Icon, process.Desc);
            var processTreeNode = await EnsureTreeNodeAsync(db, entryId, processNode.Id, null, null, process.SopSource ?? process.Desc, process.SortOrder);

            foreach (var affected in process.AffectedMechanisms.OrderBy(a => a.Id))
            {
                var mechanismNode = await EnsureBusinessNodeAsync(db, $"mechanism:{affected.MechanismCode}", affected.MechanismName, "mechanism", "", null);
                var description = JoinDescription(affected.ChangeDesc, affected.ChangeDescDetail, affected.ChangeDescDetail2, affected.InstallNote, affected.Condition);
                var affectedTreeNode = await EnsureTreeNodeAsync(db, entryId, mechanismNode.Id, processTreeNode.Id, affected.MechanismName, description, affected.Id);

                foreach (var conditionCode in SplitCodes(affected.RelatedConditions))
                {
                    if (conditionCodeToRule.TryGetValue(conditionCode, out var ruleId))
                        await EnsureBindingAsync(db, ruleId, affectedTreeNode.Id, includeDescendants: false, affected.Id, "旧制程影响关系自动绑定");
                }
            }
        }
    }

    private static async Task<BusinessNode> EnsureBusinessNodeAsync(
        SensorDbContext db,
        string code,
        string name,
        string nodeType,
        string icon,
        string? description)
    {
        var node = await db.BusinessNodes.FirstOrDefaultAsync(n => n.Code == code);
        if (node != null) return node;

        node = new BusinessNode
        {
            Code = code,
            Name = name,
            NodeType = nodeType,
            Icon = icon,
            Description = description
        };
        db.BusinessNodes.Add(node);
        await db.SaveChangesAsync();
        return node;
    }

    private static async Task<EntryTreeNode> EnsureTreeNodeAsync(
        SensorDbContext db,
        int entryId,
        int businessNodeId,
        int? parentId,
        string? displayName,
        string? descriptionOverride,
        int sortOrder)
    {
        var node = await db.EntryTreeNodes.FirstOrDefaultAsync(n =>
            n.EntryId == entryId &&
            n.BusinessNodeId == businessNodeId &&
            n.ParentId == parentId);

        if (node != null) return node;

        node = new EntryTreeNode
        {
            EntryId = entryId,
            BusinessNodeId = businessNodeId,
            ParentId = parentId,
            DisplayName = displayName,
            DescriptionOverride = descriptionOverride,
            SortOrder = sortOrder,
            InheritRules = true
        };
        db.EntryTreeNodes.Add(node);
        await db.SaveChangesAsync();
        return node;
    }

    private static async Task EnsureBindingAsync(
        SensorDbContext db,
        int ruleId,
        int treeNodeId,
        bool includeDescendants,
        int sortOrder,
        string note)
    {
        var exists = await db.RuleEntryBindings.AnyAsync(b => b.RuleId == ruleId && b.EntryTreeNodeId == treeNodeId);
        if (exists) return;

        db.RuleEntryBindings.Add(new RuleEntryBinding
        {
            RuleId = ruleId,
            EntryTreeNodeId = treeNodeId,
            IncludeDescendants = includeDescendants,
            SortOrder = sortOrder,
            Note = note
        });
        await db.SaveChangesAsync();
    }

    private static async Task ImportLegacyWorkflowToSelectorAsync(SensorDbContext db)
    {
        var selectorEntry = await EnsureEntryAsync(db, "selector", "选型配置", "", "选型配置分类树", 0);
        if (!await TableExistsAsync(db, "WorkflowEntries") || !await TableExistsAsync(db, "WorkflowNodes"))
            return;

        var nodeIdMap = await GetLegacyWorkflowNodeMapAsync(db, selectorEntry.Id);
        var selectorHasNodes = await db.EntryTreeNodes.AnyAsync(n => n.EntryId == selectorEntry.Id);
        if (selectorHasNodes)
        {
            if (await TableExistsAsync(db, "WorkflowRuleBindings"))
                await ImportLegacyWorkflowResultsAsync(db, nodeIdMap);
            return;
        }

        var entries = await QueryRowsAsync(db, """
            SELECT "Id", "Code", "Name", "Icon", "Description", "SortOrder"
            FROM "WorkflowEntries"
            ORDER BY "SortOrder", "Id"
            """);
        var nodes = await QueryRowsAsync(db, """
            SELECT "Id", "EntryId", "ParentId", "Code", "Name", "NodeType", "Description", "SortOrder"
            FROM "WorkflowNodes"
            ORDER BY "EntryId", "SortOrder", "Id"
            """);
        if (entries.Count == 0 && nodes.Count == 0) return;

        var entryRootIds = new Dictionary<int, int>();
        foreach (var row in entries)
        {
            var oldEntryId = ToInt(row["Id"]);
            var node = await EnsureBusinessNodeAsync(
                db,
                $"selector:workflow-entry:{row["Code"]}",
                ToStringValue(row["Name"]),
                "workflow-entry",
                ToStringValue(row["Icon"]),
                ToNullableString(row["Description"]));
            var treeNode = await EnsureTreeNodeAsync(
                db,
                selectorEntry.Id,
                node.Id,
                null,
                ToStringValue(row["Name"]),
                ToNullableString(row["Description"]),
                ToInt(row["SortOrder"]));
            entryRootIds[oldEntryId] = treeNode.Id;
        }

        var pendingNodes = nodes.ToList();
        while (pendingNodes.Count > 0)
        {
            var progressed = false;
            foreach (var row in pendingNodes.ToList())
            {
                var oldNodeId = ToInt(row["Id"]);
                var oldEntryId = ToInt(row["EntryId"]);
                var oldParentId = ToNullableInt(row["ParentId"]);
                if (!entryRootIds.TryGetValue(oldEntryId, out var entryRootId)) continue;
                if (oldParentId.HasValue && !nodeIdMap.ContainsKey(oldParentId.Value)) continue;

                var parentTreeNodeId = oldParentId.HasValue ? nodeIdMap[oldParentId.Value] : entryRootId;
                var businessNode = await EnsureBusinessNodeAsync(
                    db,
                    $"selector:workflow-node:{oldNodeId}",
                    ToStringValue(row["Name"]),
                    ToStringValue(row["NodeType"], "category"),
                    "",
                    ToNullableString(row["Description"]));
                var treeNode = await EnsureTreeNodeAsync(
                    db,
                    selectorEntry.Id,
                    businessNode.Id,
                    parentTreeNodeId,
                    ToStringValue(row["Name"]),
                    ToNullableString(row["Description"]),
                    ToInt(row["SortOrder"]));

                nodeIdMap[oldNodeId] = treeNode.Id;
                pendingNodes.Remove(row);
                progressed = true;
            }

            if (!progressed)
            {
                foreach (var row in pendingNodes)
                {
                    var oldNodeId = ToInt(row["Id"]);
                    var oldEntryId = ToInt(row["EntryId"]);
                    if (!entryRootIds.TryGetValue(oldEntryId, out var entryRootId)) continue;

                    var businessNode = await EnsureBusinessNodeAsync(
                        db,
                        $"selector:workflow-node:{oldNodeId}",
                        ToStringValue(row["Name"]),
                        ToStringValue(row["NodeType"], "category"),
                        "",
                        ToNullableString(row["Description"]));
                    var treeNode = await EnsureTreeNodeAsync(
                        db,
                        selectorEntry.Id,
                        businessNode.Id,
                        entryRootId,
                        ToStringValue(row["Name"]),
                        ToNullableString(row["Description"]),
                        ToInt(row["SortOrder"]));
                    nodeIdMap[oldNodeId] = treeNode.Id;
                }
                break;
            }
        }

        if (await TableExistsAsync(db, "WorkflowRuleBindings"))
            await ImportLegacyWorkflowResultsAsync(db, nodeIdMap);
    }

    private static async Task ImportLegacyWorkflowResultsAsync(SensorDbContext db, Dictionary<int, int> nodeIdMap)
    {
        var bindings = await QueryRowsAsync(db, """
            SELECT "Id", "NodeId", "RuleId", "SortOrder", "Note"
            FROM "WorkflowRuleBindings"
            ORDER BY "SortOrder", "Id"
            """);

        foreach (var binding in bindings)
        {
            var oldNodeId = ToInt(binding["NodeId"]);
            var ruleId = ToInt(binding["RuleId"]);
            if (!nodeIdMap.TryGetValue(oldNodeId, out var treeNodeId)) continue;
            var sortOrder = ToInt(binding["SortOrder"]);
            var existingResult = await db.SelectionResults
                .Include(result => result.Products)
                .FirstOrDefaultAsync(result => result.EntryTreeNodeId == treeNodeId && result.SortOrder == sortOrder);

            var ruleRows = await QueryRowsAsync(db, $"""
                SELECT "Note"
                FROM "SelectionRules"
                WHERE "Id" = {ruleId}
                LIMIT 1
                """);
            var productRows = await QueryRowsAsync(db, $"""
                SELECT "ProductId", "Quantity"
                FROM "RuleProducts"
                WHERE "RuleId" = {ruleId}
                ORDER BY "Id"
                """);

            var bindingNote = ToNullableString(binding["Note"]);
            var ruleNote = ruleRows.Count == 0 ? null : ToNullableString(ruleRows[0]["Note"]);
            var note = string.IsNullOrWhiteSpace(ruleNote) ? bindingNote : ruleNote;
            var products = productRows
                .Select(row => new SelectionResultProduct
                {
                    ProductId = ToInt(row["ProductId"]),
                    Quantity = Math.Max(ToInt(row["Quantity"]), 1)
                })
                .GroupBy(item => item.ProductId)
                .Select(group => new SelectionResultProduct
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => Math.Max(item.Quantity, 1))
                })
                .ToList();

            if (existingResult != null)
            {
                if (products.Count == 0 || existingResult.Products.Count > 0) continue;

                foreach (var product in products)
                {
                    existingResult.Products.Add(product);
                }

                await db.SaveChangesAsync();
                continue;
            }

            db.SelectionResults.Add(new SelectionResult
            {
                EntryTreeNodeId = treeNodeId,
                Note = note,
                SortOrder = sortOrder,
                Products = products
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureDefaultRuleProductsAsync(SensorDbContext db)
    {
        var defaults = SeedData.BuildDefaultRuleProducts();
        if (defaults.Count == 0) return;

        var existingRuleIds = (await db.SelectionRules.Select(rule => rule.Id).ToListAsync()).ToHashSet();
        var existingProductIds = (await db.Products.Select(product => product.Id).ToListAsync()).ToHashSet();
        var existingPairs = await db.RuleProducts
            .Select(item => new { item.RuleId, item.ProductId })
            .ToListAsync();
        var pairKeys = existingPairs
            .Select(item => $"{item.RuleId}:{item.ProductId}")
            .ToHashSet(StringComparer.Ordinal);
        var nextId = await db.RuleProducts.AnyAsync()
            ? await db.RuleProducts.MaxAsync(item => item.Id) + 1
            : 1;

        foreach (var item in defaults)
        {
            if (!existingRuleIds.Contains(item.RuleId) || !existingProductIds.Contains(item.ProductId)) continue;
            if (!pairKeys.Add($"{item.RuleId}:{item.ProductId}")) continue;

            db.RuleProducts.Add(new RuleProduct
            {
                Id = nextId++,
                RuleId = item.RuleId,
                ProductId = item.ProductId,
                Quantity = Math.Max(item.Quantity, 1)
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Dictionary<int, int>> GetLegacyWorkflowNodeMapAsync(SensorDbContext db, int selectorEntryId)
    {
        var rows = await QueryRowsAsync(db, $"""
            SELECT tree."Id", node."Code"
            FROM "EntryTreeNodes" tree
            INNER JOIN "BusinessNodes" node ON node."Id" = tree."BusinessNodeId"
            WHERE tree."EntryId" = {selectorEntryId}
              AND node."Code" LIKE 'selector:workflow-node:%'
            """);

        var map = new Dictionary<int, int>();
        foreach (var row in rows)
        {
            var code = ToStringValue(row["Code"]);
            var idText = code["selector:workflow-node:".Length..];
            if (int.TryParse(idText, out var oldNodeId))
                map[oldNodeId] = ToInt(row["Id"]);
        }
        return map;
    }

    private static async Task<bool> TableExistsAsync(SensorDbContext db, string tableName)
    {
        var rows = await QueryRowsAsync(db, $"""
            SELECT name
            FROM sqlite_master
            WHERE type = 'table' AND name = '{tableName.Replace("'", "''")}'
            """);
        return rows.Count > 0;
    }

    private static async Task<List<Dictionary<string, object?>>> QueryRowsAsync(SensorDbContext db, string sql)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
                row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
            rows.Add(row);
        }
        return rows;
    }

    private static int ToInt(object? value) => Convert.ToInt32(value ?? 0);

    private static int? ToNullableInt(object? value) => value == null ? null : Convert.ToInt32(value);

    private static string ToStringValue(object? value, string fallback = "") =>
        value?.ToString() ?? fallback;

    private static string? ToNullableString(object? value)
    {
        var text = value?.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static IEnumerable<string> SplitCodes(string? codes)
    {
        if (string.IsNullOrWhiteSpace(codes)) yield break;

        foreach (var code in codes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            yield return code;
    }

    private static string? JoinDescription(params string?[] parts)
    {
        var values = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        return values.Length == 0 ? null : string.Join("\n", values);
    }
}
