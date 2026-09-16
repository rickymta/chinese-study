using Xunit;

namespace AntFarm.Testing;

/// <summary>
/// Test cần PostgreSQL thật — tự Skip khi biến môi trường <c>AF_TEST_PG</c> chưa được đặt,
/// để `dotnet test` chạy được trên máy chưa cấu hình DB test (§5.2.0.8). F0 chưa dùng —
/// dùng từ F2 trở đi cho ApiTests cần DB.
/// </summary>
public sealed class DbFactAttribute : FactAttribute
{
    public DbFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AF_TEST_PG")))
            Skip = "Chưa đặt AF_TEST_PG — bỏ qua test cần PostgreSQL";
    }
}
