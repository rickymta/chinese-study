using FluentValidation;

namespace AntFarm.Chinese.Application.Srs;

/// <summary>Kiểm HÌNH DẠNG (§6.2) — 400 <c>VALIDATION</c>. Quy tắc nghiệp vụ (thẻ tồn tại, tạm dừng, hạn mức thẻ mới) thuộc <see cref="SrsReviewService"/> vì cần đọc DB — trả 404/422.</summary>
public sealed class ReviewCardCommandValidator : AbstractValidator<ReviewCardCommand>
{
    public ReviewCardCommandValidator()
    {
        RuleFor(x => x.ClientReviewId).NotEqual(Guid.Empty).WithMessage("clientReviewId là bắt buộc.");
        RuleFor(x => x.Rating).IsInEnum();
        RuleFor(x => x.DurationMs).InclusiveBetween(0, 600_000).When(x => x.DurationMs.HasValue);
    }
}
