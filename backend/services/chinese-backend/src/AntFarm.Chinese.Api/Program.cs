using System.Text.Json;
using System.Text.Json.Serialization;
using AntFarm.Chinese.Application;
using AntFarm.Chinese.Infrastructure;
using AntFarm.Chinese.Infrastructure.Persistence;
using AntFarm.HealthChecks;
using AntFarm.Logging;
using AntFarm.Security.Cors;
using AntFarm.Security.Errors;
using AntFarm.Security.Middleware;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddAfSerilog("chinese-backend");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration); // DbContext; ném InvalidOperationException rõ ràng nếu ConnectionStrings:Default rỗng
builder.Services.AddApplication();
builder.Services.AddAfHealthChecks(); // "self" [live]; Infrastructure thêm "postgres" [ready]
builder.Services.AddAfCors(builder.Configuration);

// F3 chèn ở đây: AddAfJwtBearer, AddAuthorization

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAfInvalidModelStateResponse(); // 400 → { error, code: "VALIDATION", details: { field: [..] } }
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "AntFarm Chinese API";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseAfSecurityHeaders();
app.UseAfCorrelationId();
app.UseAfExceptionHandler(); // AppException → status+code; còn lại 500 "Đã xảy ra lỗi nội bộ." + log Error
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
    app.MapScalarApiReference(); // /scalar/v1
}
app.UseAfCors();
// F3: app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();
app.MapAfHealthChecks();

if (app.Configuration.GetValue<bool>("AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChineseDbContext>();
    if (db.Database.GetMigrations().Any())
        await db.Database.MigrateAsync();
    // F3+: seeder chạy ở đây — bắt mọi exception, log Error, KHÔNG ném
}

Log.Information("Khởi động chinese-backend ({Env})", app.Environment.EnvironmentName);
try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "chinese-backend dừng bất thường");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
