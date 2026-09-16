namespace AntFarm.Identity.ApiTests.Infrastructure;

/// <summary>Đồng hồ giả tua được — dùng để test "ngoài cửa sổ ân hạn 30 giây" (R-A6) mà không phải Thread.Sleep 31 giây thật.</summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
