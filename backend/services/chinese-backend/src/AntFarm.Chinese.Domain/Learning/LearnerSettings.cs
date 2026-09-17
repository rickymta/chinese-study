namespace AntFarm.Chinese.Domain.Learning;

/// <summary>
/// Cài đặt học tập cá nhân (§5.1.2, migration F7_Srs) — KHÔNG có dòng ⇒ dùng
/// <see cref="Defaults"/> (GET không tự chèn dòng, PUT mới upsert, §6.2). PK = <c>UserId</c> (một
/// người học chỉ có một bộ cài đặt).
/// </summary>
public sealed class LearnerSettings
{
    public static class Defaults
    {
        public const short DailyNewCards = 10;
        public const short DailyReviewLimit = 200;
        public const decimal DesiredRetention = 0.90m;
        public const decimal TtsRate = 0.80m;
        public const bool AutoPlayAudio = true;
    }

    public Guid UserId { get; private set; }
    public short DailyNewCards { get; private set; } = Defaults.DailyNewCards;
    public short DailyReviewLimit { get; private set; } = Defaults.DailyReviewLimit;
    public decimal DesiredRetention { get; private set; } = Defaults.DesiredRetention;
    public decimal TtsRate { get; private set; } = Defaults.TtsRate;
    public bool AutoPlayAudio { get; private set; } = Defaults.AutoPlayAudio;
    public DateTime UpdatedAt { get; private set; }

    // EF Core cần constructor không tham số.
    private LearnerSettings()
    {
    }

    public static LearnerSettings CreateDefault(Guid userId, DateTime nowUtc) => new()
    {
        UserId = userId,
        DailyNewCards = Defaults.DailyNewCards,
        DailyReviewLimit = Defaults.DailyReviewLimit,
        DesiredRetention = Defaults.DesiredRetention,
        TtsRate = Defaults.TtsRate,
        AutoPlayAudio = Defaults.AutoPlayAudio,
        UpdatedAt = nowUtc
    };

    /// <summary>R7-14: đổi cài đặt CHỈ ảnh hưởng các lượt chấm SAU — không lập lịch lại thẻ cũ.</summary>
    public void Update(short dailyNewCards, short dailyReviewLimit, decimal desiredRetention, decimal ttsRate, bool autoPlayAudio, DateTime nowUtc)
    {
        DailyNewCards = dailyNewCards;
        DailyReviewLimit = dailyReviewLimit;
        DesiredRetention = desiredRetention;
        TtsRate = ttsRate;
        AutoPlayAudio = autoPlayAudio;
        UpdatedAt = nowUtc;
    }
}
