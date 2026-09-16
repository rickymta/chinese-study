using AntFarm.HealthChecks;
using AntFarm.Logging;
using AntFarm.Security.Cors;
using AntFarm.Security.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddAfSerilog("gateway");

builder.Services.AddAfHealthChecks();
builder.Services.AddAfCors(builder.Configuration);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAfCorrelationId();
app.UseAfSecurityHeaders();
app.UseAfCors();
app.MapAfHealthChecks();
app.MapReverseProxy();

app.Run();
