using Xunit;

namespace AntFarm.Testing;

/// <summary>
/// Bản <see cref="TheoryAttribute"/> của <see cref="DbFactAttribute"/> — test tham số hoá cần
/// PostgreSQL thật tự Skip khi biến môi trường <c>AF_TEST_PG</c> chưa được đặt (cùng lý do/logic
/// với <see cref="DbFactAttribute"/>, tách riêng vì xUnit không cho <c>FactAttribute</c> và
/// <c>TheoryAttribute</c> dùng chung một lớp cơ sở áp cho cả <c>[Theory]</c>).
/// </summary>
public sealed class DbTheoryAttribute : TheoryAttribute
{
    public DbTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AF_TEST_PG")))
            Skip = "Chưa đặt AF_TEST_PG — bỏ qua test cần PostgreSQL";
    }
}
