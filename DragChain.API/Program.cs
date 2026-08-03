using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using DragChain.API.Data;
using DragChain.API.Sensor.Controllers;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Security;
using DragChain.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 生产及其他非开发环境必须显式提供高强度密钥；开发/测试环境使用随机进程密钥。
var authSigningKey = AuthSigningKeyProvider.Resolve(
    Environment.GetEnvironmentVariable("DRAGCHAIN_AUTH_SIGNING_KEY"),
    builder.Environment.EnvironmentName);
AuthController.ConfigureSigningKey(authSigningKey);

// SQLite 数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=dragchain.db";
builder.Services.AddDbContext<DragChainDbContext>(options =>
    options.UseSqlite(connectionString));

var sensorConnectionString = builder.Configuration.GetConnectionString("SensorConnection")
    ?? "Data Source=sensor.db";
builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseSqlite(sensorConnectionString));

// 服务注册
builder.Services.AddScoped<ICalculationService, CalculationService>();
builder.Services.AddScoped<ITrunkingCalculationService, TrunkingCalculationService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175",
                           "http://127.0.0.1:5173", "http://127.0.0.1:5174", "http://127.0.0.1:5175")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "选型软件 API", Version = "v1", Description = "线槽、拖链、感应器选型软件后端 API" });
});

var app = builder.Build();

// 数据库初始化 + 种子数据
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DragChainDbContext>();
    context.Database.Migrate();
    await SqliteSchemaCompatibility.EnsureAsync(context);
    await CatalogSeeder.SeedAsync(context);

    var sensorContext = scope.ServiceProvider.GetRequiredService<SensorDbContext>();
    sensorContext.Database.EnsureCreated();
    await ProductMigrator.MigrateAsync(sensorContext);
    await ProcessNoteMigrator.MigrateAsync(sensorContext);
    await SelectionEntryMigrator.MigrateAsync(sensorContext);
    await RbacMigrator.MigrateAsync(sensorContext);
}

// CORS：同站点部署时不需要跨域；开发环境保留 Vite 代理/直连兼容。
app.UseCors("AllowFrontend");

// IIS 单站点部署：后端同时托管前端构建产物（wwwroot）。
app.UseDefaultFiles();
app.UseStaticFiles();

var selectionResultImageRoot = Path.Combine(app.Environment.ContentRootPath, "storage", "selection-result-images");
Directory.CreateDirectory(selectionResultImageRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(selectionResultImageRoot),
    RequestPath = "/selection-result-images"
});

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "选型软件 API v1");
});

app.UseMiddleware<RbacMiddleware>();

app.MapControllers();

// 健康检查
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

// API 路径未命中时返回明确错误，避免 SPA fallback 把 index.html 当成 JSON 返回给前端。
app.MapFallback("/api/{**path}", () => Results.NotFound(new { message = "API 接口不存在" }));

app.MapFallbackToFile("index.html");

app.Run();
