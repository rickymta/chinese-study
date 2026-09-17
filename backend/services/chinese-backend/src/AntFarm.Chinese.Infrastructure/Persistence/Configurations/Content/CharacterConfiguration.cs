using System.Text.Json;
using AntFarm.Chinese.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Content;

/// <summary>Bảng content.characters (§5.1.1, migration F6_Vocabulary).</summary>
public sealed class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("characters", "content", t =>
        {
            t.HasCheckConstraint("ck_characters_han_viet_status", "han_viet_status IN ('derived','reviewed')");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Hanzi).HasMaxLength(4).IsRequired();
        builder.Property(c => c.TraditionalVariants).IsRequired();
        builder.Property(c => c.PinyinReadings).IsRequired();
        builder.Property(c => c.HanViet).IsRequired();

        // jsonb — chỉ đọc/ghi CẢ KHỐI, không lọc SQL (§5.1.1). Npgsql KHÔNG tự hỗ trợ ghi thẳng
        // Dictionary<TKey,TValue> vào cột jsonb (ném NotSupportedException đòi "EnableDynamicJson")
        // trừ khi bật JSON động toàn cục — thay vào đó tự serialize/deserialize bằng
        // System.Text.Json qua HasConversion (an toàn, không cần cấu hình Npgsql thêm). ValueComparer
        // thủ công vì Dictionary<TKey,TValue> không override Equals theo cấu trúc (ChangeTracker mặc
        // định coi MỌI lần SaveChanges là "đã đổi" nếu không khai comparer).
        builder.Property(c => c.HanVietByPinyin)
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions))
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(BuildDictionaryComparer());

        builder.Property(c => c.HanVietStatus).HasMaxLength(16).IsRequired();
        builder.Property(c => c.Radical).HasMaxLength(4);
        builder.Property(c => c.Sources).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        // F8 (migration F8_Writing): writing_attempts/character_writing_stats trỏ FK tới CHÍNH
        // Hanzi (không có character_id) ⇒ cần khai khoá THAY THẾ (HasAlternateKey qua HasPrincipalKey
        // ở phía FK) — KHÔNG giữ thêm HasIndex(...).IsUnique() song song vì EF sẽ sinh CẢ HAI đối
        // tượng (unique constraint ak_characters_hanzi + unique index ux_characters_hanzi) cùng
        // thực thi một việc trên một cột (đã kiểm bằng `dotnet ef migrations add` rồi bỏ dòng này,
        // review F8 17/09/2026) — bản thân alternate key CŨNG là một unique index ở Postgres nên
        // không mất tính năng tra cứu nhanh theo hanzi.
        builder.HasAlternateKey(c => c.Hanzi).HasName("ak_characters_hanzi");
    }

    private static ValueComparer<Dictionary<string, string>?> BuildDictionaryComparer() => new(
        (a, b) => DictionaryEquals(a, b),
        d => d == null ? 0 : d.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key, kv.Value)),
        d => d == null ? null : new Dictionary<string, string>(d));

    private static bool DictionaryEquals(Dictionary<string, string>? a, Dictionary<string, string>? b)
    {
        if (ReferenceEquals(a, b))
            return true;
        if (a is null || b is null)
            return false;
        if (a.Count != b.Count)
            return false;

        foreach (var (key, value) in a)
            if (!b.TryGetValue(key, out var otherValue) || otherValue != value)
                return false;

        return true;
    }
}
