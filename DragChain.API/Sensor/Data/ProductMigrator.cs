using Microsoft.EntityFrameworkCore;

namespace DragChain.API.Sensor.Data;

public static class ProductMigrator
{
    public static async Task MigrateAsync(SensorDbContext db)
    {
        var columns = await db.Database.SqlQueryRaw<string>("""
            SELECT name AS Value
            FROM pragma_table_info('Products')
            """).ToListAsync();

        if (!columns.Contains("Scene"))
        {
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE Products
                ADD COLUMN Scene TEXT NULL
                """);
        }

        var codeColumnIsNotNull = await db.Database.SqlQueryRaw<int>("""
            SELECT [notnull] AS Value
            FROM pragma_table_info('Products')
            WHERE name = 'Code'
            """).FirstOrDefaultAsync();

        if (codeColumnIsNotNull == 1)
        {
            await db.Database.ExecuteSqlRawAsync("""
                DROP INDEX IF EXISTS IX_Products_Code
                """);

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS Products_New (
                    Id INTEGER NOT NULL CONSTRAINT PK_Products PRIMARY KEY AUTOINCREMENT,
                    Code TEXT NULL,
                    Model TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    Brand TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Spec TEXT NULL,
                    Scene TEXT NULL
                )
                """);

            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO Products_New (Id, Code, Model, Name, Brand, Type, Spec, Scene)
                SELECT Id,
                       NULLIF(TRIM(COALESCE(Code, '')), ''),
                       Model,
                       Name,
                       Brand,
                       Type,
                       Spec,
                       Scene
                FROM Products
                """);

            await db.Database.ExecuteSqlRawAsync("DROP TABLE Products");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE Products_New RENAME TO Products");
        }

        await db.Database.ExecuteSqlRawAsync("""
            UPDATE Products
            SET Code = 'AUTO-' || Id
            WHERE TRIM(COALESCE(Code, '')) = ''
            """);

        var duplicateCodes = await db.Database.SqlQueryRaw<string>("""
            SELECT Code AS Value
            FROM Products
            WHERE TRIM(COALESCE(Code, '')) <> ''
            GROUP BY Code
            HAVING COUNT(*) > 1
            """).ToListAsync();

        foreach (var code in duplicateCodes)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE Products
                SET Code = Code || '-' || Id
                WHERE Code = {code}
                  AND Id NOT IN (
                      SELECT MIN(Id)
                      FROM Products
                      WHERE Code = {code}
                  )
                """);
        }

        await db.Database.ExecuteSqlRawAsync("""
            DROP INDEX IF EXISTS IX_Products_Code
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Products_Code
            ON Products (Code)
            """);
    }
}
