using AntFarm.Cms.Application.Common.Revalidation;
using Microsoft.Extensions.Logging;

namespace AntFarm.Cms.Infrastructure.Revalidation;

/// <summary>
/// Hiện thực TẠM (W3a — website chưa tồn tại) của <see cref="IRevalidationNotifier"/>: chỉ log
/// Debug, không gọi mạng. W7 thay bằng <c>HttpRevalidationNotifier</c> (POST webhook website, §5.2.7).
/// </summary>
public sealed class NoopRevalidationNotifier(ILogger<NoopRevalidationNotifier> logger) : IRevalidationNotifier
{
    public Task NotifyAsync(IReadOnlyCollection<string> tags, CancellationToken ct)
    {
        logger.LogDebug("Revalidate (no-op tới W7): {Tags}", string.Join(",", tags));
        return Task.CompletedTask;
    }
}
