using AntFarm.Chinese.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Content;

/// <summary>Bảng content.import_runs (§5.1.1, migration F6_Vocabulary) — nhật ký mỗi lượt nạp học liệu.</summary>
public sealed class ImportRunConfiguration : IEntityTypeConfiguration<ImportRun>
{
    public void Configure(EntityTypeBuilder<ImportRun> builder)
    {
        builder.ToTable("import_runs", "content", t =>
        {
            t.HasCheckConstraint("ck_import_runs_status", "status IN ('succeeded','failed')");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Dataset).HasMaxLength(64).IsRequired();
        builder.Property(r => r.FileHash).HasMaxLength(64).IsRequired();
        builder.Property(r => r.ImporterVersion).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(16).IsRequired();
        builder.Property(r => r.Inserted).IsRequired();
        builder.Property(r => r.Updated).IsRequired();
        builder.Property(r => r.Unchanged).IsRequired();
        builder.Property(r => r.Invalid).IsRequired();
        builder.Property(r => r.Protected).IsRequired();
        builder.Property(r => r.StartedAt).IsRequired();
        builder.Property(r => r.FinishedAt).IsRequired();

        builder.HasIndex(r => new { r.Dataset, r.StartedAt }).IsDescending(false, true).HasDatabaseName("ix_import_runs_dataset_started");
    }
}
