namespace AntFarm.Chinese.Application.Learning;

/// <summary>GET/PUT /api/me/learning-settings (§6.2) — <c>isDefault = true</c> khi chưa có dòng learner_settings (chưa từng đổi khỏi mặc định).</summary>
public sealed record LearnerSettingsDto(
    short DailyNewCards, short DailyReviewLimit, decimal DesiredRetention, decimal TtsRate, bool AutoPlayAudio, bool IsDefault);

/// <summary>PUT /api/me/learning-settings — đủ cả 5 trường (§6.2, không hỗ trợ patch từng phần).</summary>
public sealed record UpdateLearnerSettingsCommand(
    short DailyNewCards, short DailyReviewLimit, decimal DesiredRetention, decimal TtsRate, bool AutoPlayAudio);

/// <summary>Ba số cấu hình SRS thật sự dùng để lập lịch/đếm hạn mức — đã áp mặc định nếu người dùng chưa lưu cài đặt (<see cref="LearnerSettingsService.GetEffectiveAsync"/>).</summary>
public sealed record EffectiveLearnerSettings(short DailyNewCards, short DailyReviewLimit, decimal DesiredRetention);
