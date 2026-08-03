using Microsoft.EntityFrameworkCore;

namespace DragChain.API.Sensor.Data;

public static class ProcessNoteMigrator
{
    public static async Task MigrateAsync(SensorDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ProcessNotes (
                Id INTEGER NOT NULL CONSTRAINT PK_ProcessNotes PRIMARY KEY AUTOINCREMENT,
                ProcessName TEXT NOT NULL,
                Characteristic TEXT NOT NULL DEFAULT '',
                SelectionNote TEXT NOT NULL DEFAULT ''
            )
            """);
    }
}
