using AntFarm.Cms.Domain.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Cms.Infrastructure.Persistence.Configurations.Site;

/// <summary>Bảng site.audit_logs (§5.1.2, migration W3a_SiteBasics).</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "site");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.At).IsRequired();
        builder.Property(a => a.ActorId).IsRequired();
        builder.Property(a => a.ActorEmail).HasMaxLength(254).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(64).IsRequired();
        builder.Property(a => a.TargetType).HasMaxLength(32).IsRequired();
        builder.Property(a => a.TargetId).HasMaxLength(64);
        builder.Property(a => a.Summary).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Success).IsRequired();

        builder.HasIndex(a => a.At).IsDescending().HasDatabaseName("ix_audit_logs_at_desc");
        builder.HasIndex(a => new { a.TargetType, a.TargetId }).HasDatabaseName("ix_audit_logs_target");
    }
}
