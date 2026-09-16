using Microsoft.AspNetCore.Builder;
using Serilog;

namespace AntFarm.Logging;

public static class SerilogExtensions
{
    /// <summary>
    /// Cấu hình Serilog dùng chung cho mọi service AntFarm: đọc cấu hình mức log từ
    /// appsettings ("Serilog:MinimumLevel"), ghi console kèm CorrelationId
    /// (do <c>AntFarm.Security</c> bơm vào <see cref="Serilog.Context.LogContext"/>).
    ///
    /// Chỉ <c>Enrich.FromLogContext()</c> — không rolling file, không audit sink riêng
    /// (khác MedDental): AntFarm ở giai đoạn container hoá đầu tiên, log thu qua
    /// stdout của Docker là đủ.
    /// </summary>
    public static void AddAfSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(
                outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}
