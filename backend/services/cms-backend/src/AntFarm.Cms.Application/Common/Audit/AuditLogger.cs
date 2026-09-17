using AntFarm.Cms.Application.Common.Abstractions;
using AntFarm.Cms.Domain.Site;

namespace AntFarm.Cms.Application.Common.Audit;

/// <inheritdoc cref="IAuditLogger"/>
public sealed class AuditLogger(ICmsDbContext db, TimeProvider timeProvider) : IAuditLogger
{
    public void Add(Actor actor, string action, string targetType, string? targetId, string summary, bool success = true)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        db.AuditLogs.Add(AuditLog.Create(now, actor.Id, actor.Email, action, targetType, targetId, summary, success));
    }
}
