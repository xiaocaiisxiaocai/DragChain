using System.Security.Cryptography;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Security;
using Microsoft.EntityFrameworkCore;

namespace DragChain.API.Tests;

internal static class SecurityRegressionTests
{
    private const string SecureCredentialMigrationKey = "2026-08-rbac-secure-credentials-v1";

    public static async Task RunAsync()
    {
        AssertThrows<InvalidOperationException>(
            () => AuthSigningKeyProvider.Resolve(null, "Production"),
            "生产环境缺少签名密钥时必须拒绝启动");
        AssertThrows<InvalidOperationException>(
            () => AuthSigningKeyProvider.Resolve("short-key", "Production"),
            "生产签名密钥不足 32 字节时必须拒绝启动");

        var developmentKey1 = AuthSigningKeyProvider.Resolve(null, "Development");
        var developmentKey2 = AuthSigningKeyProvider.Resolve(null, "Development");
        AssertEqual(32, developmentKey1.Length, "开发环境临时签名密钥必须具备 256 位熵");
        AssertEqual(false, CryptographicOperations.FixedTimeEquals(developmentKey1, developmentKey2), "开发环境缺省密钥必须随机生成，不能使用固定源码值");

        var passwordHash1 = RbacPasswordHasher.HashPassword("Regression-Password-1!");
        var passwordHash2 = RbacPasswordHasher.HashPassword("Regression-Password-1!");
        AssertEqual(true, RbacPasswordHasher.VerifyPassword(passwordHash1, "Regression-Password-1!", out _), "正确密码必须通过哈希验证");
        AssertEqual(false, RbacPasswordHasher.VerifyPassword(passwordHash1, "wrong-password", out _), "错误密码必须被拒绝");
        AssertEqual(false, passwordHash1 == passwordHash2, "相同密码必须使用不同随机盐");
        AssertEqual(null, RbacPasswordHasher.ValidateStrongPassword("Regression-Password-1!", required: true), "强密码必须通过统一校验");
        AssertEqual(false, RbacPasswordHasher.ValidateStrongPassword("onlyletterslong", required: true) == null, "缺少数字和符号的密码必须被拒绝");

        await AssertSecureBootstrapAsync();
        await AssertLegacyDatabaseRequiresExplicitRotationAsync();
        await AssertLegacySuperAdminRotationIsIdempotentAsync();
        await AssertLegacyAccountsDisabledWhenSafeAdminExistsAsync();
        await AssertConcurrentMigrationIsSerializedAsync();
        Console.WriteLine("PASS RBAC security regression tests");
    }

    private static async Task AssertSecureBootstrapAsync()
    {
        var environment = CaptureBootstrapEnvironment();
        var dbPath = TempDatabasePath("bootstrap");

        try
        {
            SetBootstrapEnvironment(password: null, employeeNo: "BOOT001");
            await using (var db = CreateContext(dbPath))
            {
                await db.Database.EnsureCreatedAsync();
                await AssertThrowsAsync<InvalidOperationException>(
                    () => RbacMigrator.MigrateAsync(db),
                    "空数据库未提供引导密码时必须拒绝创建账号");
                AssertEqual(0, await db.RbacUsers.CountAsync(), "缺少安全引导凭据时不能创建任何账号");
            }

            SetBootstrapEnvironment(password: "onlyletterslong", employeeNo: "BOOT001");
            await using (var db = CreateContext(dbPath))
            {
                await AssertThrowsAsync<InvalidOperationException>(
                    () => RbacMigrator.MigrateAsync(db),
                    "空数据库的引导密码必须通过统一强密码校验");
            }

            SetBootstrapEnvironment(password: "Bootstrap-Password-1!", employeeNo: "BOOT001");
            string originalHash;
            string originalStamp;
            await using (var db = CreateContext(dbPath))
            {
                await RbacMigrator.MigrateAsync(db);
                var users = await db.RbacUsers.ToListAsync();
                AssertEqual(1, users.Count, "首次初始化只能创建显式配置的引导管理员");
                AssertEqual("BOOT001", users[0].EmployeeNo, "引导管理员工号必须来自显式配置");
                AssertEqual("super_admin", users[0].Role, "引导账号必须是超级管理员");
                AssertEqual(true, users[0].Enabled, "引导账号必须启用");
                AssertEqual(true, RbacPasswordHasher.VerifyPassword(users[0].Password, "Bootstrap-Password-1!", out _), "引导密码必须以哈希形式保存");
                AssertEqual(false, string.IsNullOrWhiteSpace(users[0].SecurityStamp), "引导账号必须具备 SecurityStamp");
                AssertEqual(1, await MigrationCountAsync(db, SecureCredentialMigrationKey), "安全引导必须写入一次性迁移标记");
                originalHash = users[0].Password;
                originalStamp = users[0].SecurityStamp;
            }

            SetBootstrapEnvironment(password: "Replacement-Password-2!", employeeNo: "BOOT001");
            await using (var db = CreateContext(dbPath))
            {
                await RbacMigrator.MigrateAsync(db);
                var user = await db.RbacUsers.SingleAsync();
                AssertEqual(originalHash, user.Password, "迁移幂等重跑不能覆盖已完成引导的密码");
                AssertEqual(originalStamp, user.SecurityStamp, "迁移幂等重跑不能无故轮换 SecurityStamp");
                AssertEqual(true, RbacPasswordHasher.VerifyPassword(user.Password, "Bootstrap-Password-1!", out _), "幂等重跑后原显式密码必须保持有效");
                AssertEqual(false, RbacPasswordHasher.VerifyPassword(user.Password, "Replacement-Password-2!", out _), "幂等重跑不能采用后来残留的环境密码");
                AssertEqual(1, await MigrationCountAsync(db, SecureCredentialMigrationKey), "安全迁移标记不能重复写入");
            }
        }
        finally
        {
            RestoreBootstrapEnvironment(environment);
            DeleteDatabaseFiles(dbPath);
        }
    }

    private static async Task AssertLegacyDatabaseRequiresExplicitRotationAsync()
    {
        var environment = CaptureBootstrapEnvironment();
        var dbPath = TempDatabasePath("legacy-explicit");

        try
        {
            await using (var db = CreateContext(dbPath))
            {
                await CreateLegacyRbacSchemaAsync(db);
                await InsertLegacyUserAsync(db, "S0001", "旧超级管理员", "Legacy-Credential-A1!", "super_admin", enabled: true);
                await InsertLegacyUserAsync(db, "U0001", "旧普通用户", "Legacy-Credential-B1!", "user", enabled: true);
            }

            SetBootstrapEnvironment(password: null, employeeNo: null);
            await using (var db = CreateContext(dbPath))
            {
                await AssertThrowsAsync<InvalidOperationException>(
                    () => RbacMigrator.MigrateAsync(db),
                    "只有旧固定账号的数据库未提供轮换密码时必须拒绝启动");
                AssertEqual(0, await MigrationCountAsync(db, SecureCredentialMigrationKey), "轮换失败时不能写安全迁移标记");
                AssertEqual(2, await db.RbacUsers.CountAsync(user => user.Enabled), "轮换失败不能伪装成已完成处置");
            }

            SetBootstrapEnvironment(password: "Recovery-Password-1!", employeeNo: "RECOVERY001", name: "恢复管理员");
            string recoveryHash;
            string recoveryStamp;
            await using (var db = CreateContext(dbPath))
            {
                await RbacMigrator.MigrateAsync(db);
                var users = await db.RbacUsers.OrderBy(user => user.EmployeeNo).ToListAsync();
                var recovery = users.Single(user => user.EmployeeNo == "RECOVERY001");
                AssertEqual(true, recovery.Enabled, "显式恢复管理员必须启用");
                AssertEqual("super_admin", recovery.Role, "显式恢复管理员必须获得超级管理员角色");
                AssertEqual(true, RbacPasswordHasher.VerifyPassword(recovery.Password, "Recovery-Password-1!", out _), "显式轮换密码必须哈希保存");
                AssertEqual(false, string.IsNullOrWhiteSpace(recovery.SecurityStamp), "显式恢复管理员必须具备 SecurityStamp");
                AssertEqual(true, users.Where(user => user.EmployeeNo is "S0001" or "U0001").All(user => !user.Enabled), "显式恢复后必须禁用全部其他旧固定账号");
                AssertEqual(false, RbacPasswordHasher.VerifyPassword(users.Single(user => user.EmployeeNo == "S0001").Password, "Legacy-Credential-A1!", out _), "禁用旧超级管理员时必须销毁旧凭据");
                AssertEqual(false, RbacPasswordHasher.VerifyPassword(users.Single(user => user.EmployeeNo == "U0001").Password, "Legacy-Credential-B1!", out _), "禁用旧普通账号时必须销毁旧凭据");
                AssertEqual(true, users.All(user => RbacPasswordHasher.IsHashedPassword(user.Password)), "旧库中的所有明文密码必须升级为哈希");
                AssertEqual(true, users.All(user => !string.IsNullOrWhiteSpace(user.SecurityStamp)), "旧库补列后所有账号必须随机填充 SecurityStamp");
                AssertEqual(1, await MigrationCountAsync(db, SecureCredentialMigrationKey), "显式轮换必须写安全迁移标记");
                recoveryHash = recovery.Password;
                recoveryStamp = recovery.SecurityStamp;
            }

            SetBootstrapEnvironment(password: "Later-Password-2!", employeeNo: "RECOVERY001", name: "不应覆盖");
            await using (var db = CreateContext(dbPath))
            {
                await RbacMigrator.MigrateAsync(db);
                var recovery = await db.RbacUsers.SingleAsync(user => user.EmployeeNo == "RECOVERY001");
                AssertEqual(recoveryHash, recovery.Password, "安全迁移重跑不能再次覆盖已轮换密码");
                AssertEqual(recoveryStamp, recovery.SecurityStamp, "安全迁移重跑不能再次覆盖已轮换 SecurityStamp");
                AssertEqual(true, RbacPasswordHasher.VerifyPassword(recovery.Password, "Recovery-Password-1!", out _), "幂等重跑必须保留首次轮换密码");
                AssertEqual(1, await MigrationCountAsync(db, SecureCredentialMigrationKey), "显式轮换迁移必须保持幂等");
            }
        }
        finally
        {
            RestoreBootstrapEnvironment(environment);
            DeleteDatabaseFiles(dbPath);
        }
    }

    private static async Task AssertLegacySuperAdminRotationIsIdempotentAsync()
    {
        var environment = CaptureBootstrapEnvironment();
        var dbPath = TempDatabasePath("legacy-rotate");

        try
        {
            await using (var db = CreateContext(dbPath))
            {
                await CreateLegacyRbacSchemaAsync(db);
                await InsertLegacyUserAsync(db, "S0001", "旧超级管理员", "Legacy-Credential-C1!", "super_admin", enabled: true);
                await InsertLegacyUserAsync(db, "admin", "旧管理员", "Legacy-Credential-D1!", "admin", enabled: true);
            }

            SetBootstrapEnvironment(password: "Rotated-Legacy-Password-1!", employeeNo: null);
            await using (var db = CreateContext(dbPath))
            {
                await RbacMigrator.MigrateAsync(db);
                var legacySuperAdmin = await db.RbacUsers.SingleAsync(user => user.EmployeeNo == "S0001");
                var otherLegacy = await db.RbacUsers.SingleAsync(user => user.EmployeeNo == "admin");
                AssertEqual(true, legacySuperAdmin.Enabled, "未指定工号时必须轮换并保留旧超级管理员");
                AssertEqual(true, RbacPasswordHasher.VerifyPassword(legacySuperAdmin.Password, "Rotated-Legacy-Password-1!", out _), "旧超级管理员必须采用显式强密码");
                AssertEqual(false, otherLegacy.Enabled, "轮换旧超级管理员后必须禁用其他旧固定账号");
                AssertEqual(false, RbacPasswordHasher.VerifyPassword(otherLegacy.Password, "Legacy-Credential-D1!", out _), "被禁用的其他旧固定账号必须销毁旧凭据");
            }
        }
        finally
        {
            RestoreBootstrapEnvironment(environment);
            DeleteDatabaseFiles(dbPath);
        }
    }

    private static async Task AssertLegacyAccountsDisabledWhenSafeAdminExistsAsync()
    {
        var environment = CaptureBootstrapEnvironment();
        var dbPath = TempDatabasePath("legacy-disable");

        try
        {
            await using (var db = CreateContext(dbPath))
            {
                await CreateLegacyRbacSchemaAsync(db);
                await InsertLegacyUserAsync(db, "S0001", "旧超级管理员", "Legacy-Credential-E1!", "super_admin", enabled: true);
                await InsertLegacyUserAsync(db, "U0001", "旧普通用户", "Legacy-Credential-F1!", "user", enabled: true);
                await InsertLegacyUserAsync(db, "SAFE001", "现有安全管理员", "Existing-Safe-Password-1!", "super_admin", enabled: true);
            }

            SetBootstrapEnvironment(password: null, employeeNo: null);
            await using (var db = CreateContext(dbPath))
            {
                await RbacMigrator.MigrateAsync(db);
                var users = await db.RbacUsers.ToListAsync();
                AssertEqual(true, users.Single(user => user.EmployeeNo == "SAFE001").Enabled, "已有非旧超级管理员必须保持启用");
                AssertEqual(true, users.Where(user => user.EmployeeNo is "S0001" or "U0001").All(user => !user.Enabled), "存在安全超级管理员时必须禁用全部旧固定账号");
                AssertEqual(false, RbacPasswordHasher.VerifyPassword(users.Single(user => user.EmployeeNo == "S0001").Password, "Legacy-Credential-E1!", out _), "自动禁用旧超级管理员时必须销毁旧凭据");
                AssertEqual(false, RbacPasswordHasher.VerifyPassword(users.Single(user => user.EmployeeNo == "U0001").Password, "Legacy-Credential-F1!", out _), "自动禁用旧普通账号时必须销毁旧凭据");
                AssertEqual(true, users.All(user => RbacPasswordHasher.IsHashedPassword(user.Password)), "自动禁用路径也必须升级旧明文密码");
                AssertEqual(true, users.All(user => !string.IsNullOrWhiteSpace(user.SecurityStamp)), "自动禁用路径必须为全部旧库账号补 SecurityStamp");
                AssertEqual(1, await MigrationCountAsync(db, SecureCredentialMigrationKey), "自动禁用路径必须写安全迁移标记");
            }
        }
        finally
        {
            RestoreBootstrapEnvironment(environment);
            DeleteDatabaseFiles(dbPath);
        }
    }

    private static async Task AssertConcurrentMigrationIsSerializedAsync()
    {
        var environment = CaptureBootstrapEnvironment();
        var dbPath = TempDatabasePath("concurrent");

        try
        {
            await using (var db = CreateContext(dbPath))
            {
                await CreateLegacyRbacSchemaAsync(db);
                await InsertLegacyUserAsync(
                    db,
                    "S0001",
                    "并发迁移旧管理员",
                    "Concurrent-Legacy-Password-1!",
                    "super_admin",
                    enabled: true);
            }

            SetBootstrapEnvironment(
                password: "Concurrent-Rotation-Password-1!",
                employeeNo: "CONCURRENT001",
                name: "并发迁移管理员");

            var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task RunMigrationAsync() => Task.Run(async () =>
            {
                await start.Task;
                await using var db = CreateContext(dbPath);
                await RbacMigrator.MigrateAsync(db);
            });

            var first = RunMigrationAsync();
            var second = RunMigrationAsync();
            start.SetResult(true);
            await Task.WhenAll(first, second);

            await using (var db = CreateContext(dbPath))
            {
                var users = await db.RbacUsers.OrderBy(user => user.EmployeeNo).ToListAsync();
                AssertEqual(2, users.Count, "并发首次迁移不能重复创建引导账号");
                AssertEqual(1, users.Count(user => user.EmployeeNo == "CONCURRENT001"), "并发首次迁移只能创建一个显式管理员");
                AssertEqual(1, await MigrationCountAsync(db, SecureCredentialMigrationKey), "并发首次迁移只能提交一个安全迁移标记");

                var recovery = users.Single(user => user.EmployeeNo == "CONCURRENT001");
                AssertEqual(true, recovery.Enabled, "并发迁移后的显式管理员必须启用");
                AssertEqual(true, RbacPasswordHasher.VerifyPassword(recovery.Password, "Concurrent-Rotation-Password-1!", out _), "并发迁移后的显式密码必须有效");
                AssertEqual(false, string.IsNullOrWhiteSpace(recovery.SecurityStamp), "并发迁移后的显式管理员必须具备 SecurityStamp");

                var legacy = users.Single(user => user.EmployeeNo == "S0001");
                AssertEqual(false, legacy.Enabled, "并发迁移后旧固定管理员必须停用");
                AssertEqual(false, RbacPasswordHasher.VerifyPassword(legacy.Password, "Concurrent-Legacy-Password-1!", out _), "并发迁移后必须销毁旧固定凭据");
            }
        }
        finally
        {
            RestoreBootstrapEnvironment(environment);
            DeleteDatabaseFiles(dbPath);
        }
    }

    private static async Task CreateLegacyRbacSchemaAsync(SensorDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE RbacUsers (
                Id INTEGER NOT NULL CONSTRAINT PK_RbacUsers PRIMARY KEY AUTOINCREMENT,
                EmployeeNo TEXT NOT NULL,
                Name TEXT NOT NULL,
                Password TEXT NOT NULL,
                Role TEXT NOT NULL DEFAULT 'user',
                Enabled INTEGER NOT NULL DEFAULT 1
            )
            """);
    }

    private static async Task InsertLegacyUserAsync(
        SensorDbContext db,
        string employeeNo,
        string name,
        string password,
        string role,
        bool enabled)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO RbacUsers (EmployeeNo, Name, Password, Role, Enabled)
            VALUES ({employeeNo}, {name}, {password}, {role}, {enabled})
            """);
    }

    private static async Task<int> MigrationCountAsync(SensorDbContext db, string key)
    {
        return await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM RbacMigrationHistory WHERE Key = {key}")
            .SingleAsync();
    }

    private static SensorDbContext CreateContext(string dbPath)
    {
        var options = new DbContextOptionsBuilder<SensorDbContext>()
            .UseSqlite($"Data Source={dbPath};Default Timeout=30")
            .Options;
        return new SensorDbContext(options);
    }

    private static string TempDatabasePath(string suffix) =>
        Path.Combine(Path.GetTempPath(), $"dragchain-rbac-{suffix}-{Guid.NewGuid():N}.db");

    private static BootstrapEnvironment CaptureBootstrapEnvironment() => new(
        Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_PASSWORD"),
        Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_EMPLOYEE_NO"),
        Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_NAME"));

    private static void SetBootstrapEnvironment(string? password, string? employeeNo, string? name = null)
    {
        Environment.SetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_PASSWORD", password);
        Environment.SetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_EMPLOYEE_NO", employeeNo);
        Environment.SetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_NAME", name);
    }

    private static void RestoreBootstrapEnvironment(BootstrapEnvironment environment)
    {
        Environment.SetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_PASSWORD", environment.Password);
        Environment.SetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_EMPLOYEE_NO", environment.EmployeeNo);
        Environment.SetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_NAME", environment.Name);
    }

    private static void DeleteDatabaseFiles(string dbPath)
    {
        foreach (var path in new[] { dbPath, $"{dbPath}-wal", $"{dbPath}-shm" })
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // SQLite 连接释放可能稍有延迟，临时文件可由系统清理。
            }
        }
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static async Task AssertThrowsAsync<TException>(Func<Task> action, string message)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message}：期望 {expected}，实际 {actual}");
    }

    private sealed record BootstrapEnvironment(string? Password, string? EmployeeNo, string? Name);
}
