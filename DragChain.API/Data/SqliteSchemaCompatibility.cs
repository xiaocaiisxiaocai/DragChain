using System.Data;
using Microsoft.EntityFrameworkCore;

namespace DragChain.API.Data;

public static class SqliteSchemaCompatibility
{
    public static async Task EnsureAsync(DragChainDbContext context)
    {
        await EnsureMeCatalogColumnsAsync(context);
        await EnsureTrunkingCatalogShapeAsync(context);
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

        if (!hasLegacyColumns) return;

        // 早期迁移保留了旧字段且带 NOT NULL，当前种子数据只写新字段；重建表以匹配当前模型。
        await context.Database.ExecuteSqlRawAsync("""
            PRAGMA foreign_keys=OFF;
            DROP TABLE IF EXISTS "__TrunkingCatalog_new";
            CREATE TABLE "__TrunkingCatalog_new" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_TrunkingCatalog" PRIMARY KEY AUTOINCREMENT,
                "Model" TEXT NOT NULL,
                "Width" decimal(8,2) NOT NULL,
                "Height" decimal(8,2) NOT NULL,
                "CrossSection" decimal(10,2) NOT NULL
            );
            INSERT INTO "__TrunkingCatalog_new" ("Id", "Model", "Width", "Height", "CrossSection")
            SELECT "Id", "Model", "Width", "Height", "CrossSection"
            FROM "TrunkingCatalog";
            DROP TABLE "TrunkingCatalog";
            ALTER TABLE "__TrunkingCatalog_new" RENAME TO "TrunkingCatalog";
            CREATE INDEX "IX_TrunkingCatalog_Model" ON "TrunkingCatalog" ("Model");
            PRAGMA foreign_keys=ON;
            """);
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
