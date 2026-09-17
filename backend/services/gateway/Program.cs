using AntFarm.HealthChecks;
using AntFarm.Logging;
using AntFarm.Security.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddAfSerilog("gateway");

builder.Services.AddAfHealthChecks();
// KHÔNG AddAfCors/UseAfCors ở gateway: middleware CORS tự trả lời preflight OPTIONS (204, không header)
// nên identity-service không bao giờ nhận được ⇒ trình duyệt chặn đăng nhập chéo subdomain.
// CORS chỉ đặt ở identity-service; gateway chuyển tiếp nguyên vẹn, kể cả OPTIONS.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAfCorrelationId();
app.UseAfSecurityHeaders();
app.MapAfHealthChecks();
app.MapReverseProxy();

app.Run();
