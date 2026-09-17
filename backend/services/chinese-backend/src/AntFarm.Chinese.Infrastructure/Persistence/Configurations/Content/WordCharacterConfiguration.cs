using AntFarm.Chinese.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntFarm.Chinese.Infrastructure.Persistence.Configurations.Content;

/// <summary>Bảng content.word_characters (§5.1.1, migration F6_Vocabulary) — khoá chính ghép (word_id, position).</summary>
public sealed class WordCharacterConfiguration : IEntityTypeConfiguration<WordCharacter>
{
    public void Configure(EntityTypeBuilder<WordCharacter> builder)
    {
        builder.ToTable("word_characters", "content");

        builder.HasKey(wc => new { wc.WordId, wc.Position });

        builder.Property(wc => wc.Position).IsRequired();

        builder.HasOne<Word>().WithMany().HasForeignKey(wc => wc.WordId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Character>().WithMany().HasForeignKey(wc => wc.CharacterId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(wc => wc.CharacterId).HasDatabaseName("ix_word_characters_character");
    }
}
