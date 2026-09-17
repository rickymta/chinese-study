using System.Text.RegularExpressions;
using AntFarm.Chinese.Application.Pinyin.Dtos;
using AntFarm.Chinese.Domain.Pinyin;
using FluentValidation;

namespace AntFarm.Chinese.Application.Pinyin;

/// <summary>
/// Kiểm HÌNH DẠNG yêu cầu nộp bài (§5.2.1) — lỗi ở đây trả 400 <c>VALIDATION</c>. Kiểm NGHIỆP VỤ
/// (âm tiết có tồn tại, thanh có chữ minh hoạ, cửa sổ thời gian hợp lệ) thuộc <see cref="ToneDrillService"/>
/// vì cần đọc catalog/CSDL — trả 422, không phải 400.
/// </summary>
public sealed partial class SubmitToneDrillRequestValidator : AbstractValidator<SubmitToneDrillRequest>
{
    [GeneratedRegex("^[a-z]{1,6}$")]
    private static partial Regex SyllablePattern();

    [GeneratedRegex(@"^\p{IsCJKUnifiedIdeographs}$")]
    private static partial Regex HanziPattern();

    public SubmitToneDrillRequestValidator()
    {
        RuleFor(x => x.ClientSessionId).NotEmpty();
        RuleFor(x => x.Mode).IsInEnum();

        // D38: chuỗi thời gian JSON không có 'Z' ⇒ System.Text.Json gán Kind=Unspecified; có OFFSET
        // SỐ (vd "+07:00", không phải 'Z') ⇒ Kind=Local (không phải Utc — .NET quy đổi offset số
        // thành giờ ĐỊA PHƯƠNG của máy chạy, chỉ 'Z' mới ra thẳng Kind=Utc) — cả hai trường hợp đều
        // bị từ chối ở đây, tránh Kind≠Utc lọt vào Npgsql timestamptz lúc ghi DB (đã kiểm chứng bằng
        // JsonSerializer thật, review F5 17/09/2026).
        RuleFor(x => x.StartedAt)
            .Must(d => d.Kind == DateTimeKind.Utc)
            .WithMessage("startedAt phải là chuỗi ISO-8601 kết thúc bằng 'Z' (vd 2026-09-16T23:30:00Z) — offset số (+07:00) không được chấp nhận.");
        RuleFor(x => x.FinishedAt)
            .Must(d => d.Kind == DateTimeKind.Utc)
            .WithMessage("finishedAt phải là chuỗi ISO-8601 kết thúc bằng 'Z' (vd 2026-09-16T23:30:00Z) — offset số (+07:00) không được chấp nhận.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("items không được rỗng.")
            .Must(items => items.Count <= 100).WithMessage("items tối đa 100 câu.");

        RuleForEach(x => x.Items).Custom(ValidateItemShape);
    }

    private static void ValidateItemShape(SubmitToneDrillItemRequest? item, ValidationContext<SubmitToneDrillRequest> context)
    {
        // JSON cho phép phần tử mảng là null (vd "items": [null, {...}]) — System.Text.Json vẫn
        // deserialize thành công (list chứa null), truy cập item.Parts sẽ NullReferenceException
        // (⇒ 500) nếu không chặn ở đây trước (review F5 17/09/2026: item rỗng phải ra 400, không 500).
        if (item is null)
        {
            context.AddFailure(context.PropertyPath, "Mỗi câu (item) trong items không được null.");
            return;
        }

        if (item.ResponseMs is < 0 or > 600_000)
            context.AddFailure($"{context.PropertyPath}.responseMs", "responseMs phải trong khoảng 0..600000 hoặc bỏ trống.");
        if (item.ReplayCount is < 0 or > 100)
            context.AddFailure($"{context.PropertyPath}.replayCount", "replayCount phải trong khoảng 0..100.");

        var mode = context.InstanceToValidate.Mode;
        var expectedPartCount = mode == ToneDrillMode.ListenTone ? 1 : 2;

        if (item.Parts is null)
        {
            context.AddFailure($"{context.PropertyPath}.parts", "parts không được null.");
            return;
        }

        if (item.Parts.Count != expectedPartCount)
        {
            var modeLabel = mode == ToneDrillMode.ListenTone ? "listen_tone" : "tone_pair";
            context.AddFailure($"{context.PropertyPath}.parts", $"Chế độ {modeLabel} phải có đúng {expectedPartCount} phần mỗi câu.");
            return;
        }

        for (var i = 0; i < item.Parts.Count; i++)
        {
            var part = item.Parts[i];
            var prefix = $"{context.PropertyPath}.parts[{i}]";

            if (!SyllablePattern().IsMatch(part.Syllable))
                context.AddFailure($"{prefix}.syllable", $"Âm tiết '{part.Syllable}' không đúng định dạng.");
            if (!HanziPattern().IsMatch(part.Hanzi))
                context.AddFailure($"{prefix}.hanzi", "hanzi phải là đúng MỘT chữ Hán.");
            if (part.ExpectedTone is < 1 or > 4)
                context.AddFailure($"{prefix}.expectedTone", "expectedTone phải trong khoảng 1..4.");
            if (part.AnsweredTone is < 1 or > 4)
                context.AddFailure($"{prefix}.answeredTone", "answeredTone phải trong khoảng 1..4.");
        }

        if (mode == ToneDrillMode.TonePair && item.Parts.Count == 2)
        {
            var first = item.Parts[0];
            var second = item.Parts[1];

            if (string.Equals(first.Syllable, second.Syllable, StringComparison.Ordinal))
                context.AddFailure($"{context.PropertyPath}.parts", "Hai âm tiết trong một câu cặp thanh phải khác nhau.");

            // R5-7: loại tổ hợp 3-3 (TTS sẽ biến điệu 2-3, gây chấm sai oan).
            if (first.ExpectedTone == 3 && second.ExpectedTone == 3)
                context.AddFailure($"{context.PropertyPath}.parts", "Không được ghép hai âm tiết cùng thanh 3 trong một câu cặp thanh.");
        }
    }
}
