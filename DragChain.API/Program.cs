using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Services;

var builder = WebApplication.CreateBuilder(args);

// SQLite 数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=dragchain.db";
builder.Services.AddDbContext<DragChainDbContext>(options =>
    options.UseSqlite(connectionString));

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
    c.SwaggerDoc("v1", new() { Title = "拖鏈選型 API", Version = "v1", Description = "拖鏈選型計算工具後端 API" });
});

var app = builder.Build();

// 数据库初始化 + 种子数据
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DragChainDbContext>();
    context.Database.Migrate();
    await CatalogSeeder.SeedAsync(context);
}

// CORS
app.UseCors("AllowFrontend");

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "拖鏈選型 API v1");
});

app.MapControllers();

// 健康检查
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.Run();
