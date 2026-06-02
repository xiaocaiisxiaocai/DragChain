using System.Data;
using Microsoft.EntityFrameworkCore;

namespace DragChain.API.Data;

public static class SqliteSchemaCompatibility
{
    public static async Task EnsureAsync(DragChainDbContext context)
    {
        await EnsureMeCatalogColumnsAsync(context);
        await EnsureTrunkingCatalogShapeAsync(context);
        await EnsurePipeComponentTablesAsync(context);
        await EnsureModuleAndComponentItemLayerColumnsAsync(context);
    }

    private static async Task EnsureMeCatalogColumnsAsync(DragChainDbContext context)
    {
        var columns = await GetColumnsAsync(context, "MeCatalog");
        if (!columns.Contains("FunctionSelect"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "MeCatalog" ADD COLUMN "FunctionSelect" TEXT NOT NULL DEFAULT '';
                """);
        }

        if (!columns.Contains("MountingH1"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "MeCatalog" ADD COLUMN "MountingH1" TEXT NOT NULL DEFAULT '';
                """);
        }
    }

    private static async Task EnsureTrunkingCatalogShapeAsync(DragChainDbContext context)
    {
        var columns = await GetColumnsAsync(context, "TrunkingCatalog");
        var hasLegacyColumns = columns.Contains("InnerWidth")
            || columns.Contains("InnerHeight")
            || columns.Contains("Material")
            || columns.Contains("Remarks");

        if (hasLegacyColumns)
        {
            // 早期迁移保留了旧字段且带 NOT NULL，当前种子数据只写新字段；重建表以匹配当前模型。
            await context.Database.ExecuteSqlRawAsync("""
                PRAGMA foreign_keys=OFF;
                DROP TABLE IF EXISTS "__TrunkingCatalog_new";
                CREATE TABLE "__TrunkingCatalog_new" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_TrunkingCatalog" PRIMARY KEY AUTOINCREMENT,
                    "Model" TEXT NOT NULL,
                    "Width" decimal(8,2) NOT NULL,
                    "Height" decimal(8,2) NOT NULL,
                    "CrossSection" decimal(10,2) NOT NULL,
                    "FillRatioLimit" decimal(5,2) NOT NULL DEFAULT 0.6
                );
                INSERT INTO "__TrunkingCatalog_new" ("Id", "Model", "Width", "Height", "CrossSection", "FillRatioLimit")
                SELECT "Id", "Model", "Width", "Height", "CrossSection", 0.6
                FROM "TrunkingCatalog";
                DROP TABLE "TrunkingCatalog";
                ALTER TABLE "__TrunkingCatalog_new" RENAME TO "TrunkingCatalog";
                CREATE INDEX "IX_TrunkingCatalog_Model" ON "TrunkingCatalog" ("Model");
                PRAGMA foreign_keys=ON;
                """);
        }

        columns = await GetColumnsAsync(context, "TrunkingCatalog");
        if (!columns.Contains("FillRatioLimit"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                ALTER TABLE "TrunkingCatalog" ADD COLUMN "FillRatioLimit" decimal(5,2) NOT NULL DEFAULT 0.6;
                """);
        }
    }

    private static async Task EnsurePipeComponentTablesAsync(DragChainDbContext context)
    {
        // 元件库与模块库同构；老库升级时在启动阶段补齐独立表。
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "PipeComponents" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PipeComponents" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "Description" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "PipeComponentItems" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PipeComponentItems" PRIMARY KEY AUTOINCREMENT,
                "PipeComponentId" INTEGER NOT NULL,
                "PipeTypeId" INTEGER NOT NULL,
                "Qty" INTEGER NOT NULL,
                CONSTRAINT "FK_PipeComponentItems_PipeComponents_PipeComponentId" FOREIGN KEY ("PipeComponentId") REFERENCES "PipeComponents" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_PipeComponentItems_PipeTypes_PipeTypeId" FOREIGN KEY ("PipeTypeId") REFERENCES "PipeTypes" ("Id") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_PipeComponents_Name" ON "PipeComponents" ("Name");
            CREATE INDEX IF NOT EXISTS "IX_PipeComponentItems_PipeComponentId" ON "PipeComponentItems" ("PipeComponentId");
            CREATE INDEX IF NOT EXISTS "IX_PipeComponentItems_PipeTypeId" ON "PipeComponentItems" ("PipeTypeId");
            """);
    }

    private static async Task EnsureModuleAndComponentItemLayerColumnsAsync(DragChainDbContext context)
    {
        await EnsureColumnAsync(context, "PipeModuleItems", "Layer", """
            ALTER TABLE "PipeModuleItems" ADD COLUMN "Layer" TEXT NOT NULL DEFAULT 'top';
            """);
        await EnsureColumnAsync(context, "PipeComponentItems", "Layer", """
            ALTER TABLE "PipeComponentItems" ADD COLUMN "Layer" TEXT NOT NULL DEFAULT 'top';
            """);
        await context.Database.ExecuteSqlRawAsync("""
            UPDATE "PipeModuleItems" SET "Layer" = 'top' WHERE "Layer" IS NULL OR "Layer" NOT IN ('top', 'bottom');
            UPDATE "PipeComponentItems" SET "Layer" = 'top' WHERE "Layer" IS NULL OR "Layer" NOT IN ('top', 'bottom');
            """);
    }

    private static async Task EnsureColumnAsync(
        DragChainDbContext context,
        string tableName,
        string columnName,
        string alterSql)
    {
        var columns = await GetColumnsAsync(context, tableName);
        if (!columns.Contains(columnName))
        {
            await context.Database.ExecuteSqlRawAsync(alterSql);
        }
    }

    private static async Task<HashSet<string>> GetColumnsAsync(DragChainDbContext context, string tableName)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;
        if (shouldClose) await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(reader.GetOrdinal("name")));
            }
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }

        return columns;
    }
}
