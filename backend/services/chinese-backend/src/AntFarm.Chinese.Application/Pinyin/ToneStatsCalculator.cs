using AntFarm.Chinese.Application.Pinyin.Dtos;

namespace AntFarm.Chinese.Application.Pinyin;

/// <summary>Một phần (câu hoặc nửa câu) trong cửa sổ thống kê — hàng thô đọc từ <c>learning.tone_drill_answers</c>.</summary>
public sealed record ToneAnswerRow(int ExpectedTone, int AnsweredTone, bool IsCorrect);

/// <summary>
/// Hàm THUẦN áp quy tắc thống kê R5-13 — tách khỏi <see cref="ToneStatsService"/> (đọc DB) để unit
/// test không cần DB thật.
/// </summary>
public static class ToneStatsCalculator
{
    public const int WindowSize = 200;

    /// <summary>Ngưỡng để một thanh được đưa vào <c>recommendedFocus</c> (R5-13).</summary>
    private const int FocusMinTotal = 10;
    private const double FocusMaxAccuracy = 0.8;

    /// <summary>Ngưỡng "xong G0" (§1.3): mỗi thanh có ≥ 20 câu VÀ độ chính xác ≥ 0,85.</summary>
    private const int G0MinTotal = 20;
    private const double G0MinAccuracy = 0.85;

    /// <param name="windowRows">Hợp của 4 cửa sổ (mỗi thanh tối đa <see cref="WindowSize"/> phần gần nhất) — đã lọc/sắp/giới hạn ở <see cref="ToneStatsService"/>.</param>
    /// <param name="totalAnswered">Tổng số PHẦN mọi thời gian (không giới hạn cửa sổ).</param>
    public static ToneStatsResponse Calculate(
        IReadOnlyList<ToneAnswerRow> windowRows, int totalAnswered, int sessionsCount, DateTime? lastSessionAt)
    {
        var byTone = new Dictionary<string, ToneStatDto>(4);
        var sumTotal = 0;
        var sumCorrect = 0;

        for (var tone = 1; tone <= 4; tone++)
        {
            var total = 0;
            var correct = 0;
            foreach (var row in windowRows)
            {
                if (row.ExpectedTone != tone)
                    continue;
                total++;
                if (row.IsCorrect)
                    correct++;
            }

            double? accuracy = total == 0 ? null : (double)correct / total;
            byTone[tone.ToString()] = new ToneStatDto(total, correct, accuracy);
            sumTotal += total;
            sumCorrect += correct;
        }

        var confusionCounts = new Dictionary<(int Expected, int Answered), int>();
        foreach (var row in windowRows)
        {
            if (row.IsCorrect)
                continue;
            var key = (row.ExpectedTone, row.AnsweredTone);
            confusionCounts[key] = confusionCounts.GetValueOrDefault(key) + 1;
        }

        var confusions = confusionCounts
            .Select(kv => new ConfusionDto(kv.Key.Expected, kv.Key.Answered, kv.Value))
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Expected)
            .ThenBy(c => c.Answered)
            .Take(5)
            .ToList();

        var recommendedFocus = byTone
            .Where(kv => kv.Value.Total >= FocusMinTotal && kv.Value.Accuracy is not null && kv.Value.Accuracy < FocusMaxAccuracy)
            .OrderBy(kv => kv.Value.Accuracy)
            .ThenBy(kv => int.Parse(kv.Key))
            .Select(kv => int.Parse(kv.Key))
            .ToList();

        var g0Reached = byTone.Values.All(v => v.Total >= G0MinTotal && v.Accuracy is not null && v.Accuracy >= G0MinAccuracy);

        double? overallAccuracy = sumTotal == 0 ? null : (double)sumCorrect / sumTotal;

        return new ToneStatsResponse(
            totalAnswered, sessionsCount, lastSessionAt, WindowSize, overallAccuracy,
            byTone, confusions, recommendedFocus, g0Reached);
    }
}
