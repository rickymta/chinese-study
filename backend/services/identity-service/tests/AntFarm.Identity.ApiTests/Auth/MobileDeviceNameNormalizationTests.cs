using System.Reflection;
using AntFarm.Identity.Api.Features.Auth;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.ApiTests.Auth;

/// <summary>
/// G4 (review M1) — <c>MobileAuthController.NormalizeDeviceName</c> (RM-A6) lọc ký tự điều khiển
/// (category <c>Cc</c>) VÀ ký tự định dạng Unicode (category <c>Cf</c>: zero-width, đánh dấu
/// chiều chữ như U+202E) — không hiển thị nhưng có thể dùng để giả mạo/che nội dung tên thiết bị.
/// Gọi phương thức PRIVATE STATIC THUẦN qua reflection: đây là test THUẦN THUẬT TOÁN, không cần
/// DB/HTTP; logic quá nhỏ để tách khỏi controller mỏng thành một lớp Application riêng.
/// </summary>
public class MobileDeviceNameNormalizationTests
{
    private static readonly MethodInfo Method = typeof(MobileAuthController)
        .GetMethod("NormalizeDeviceName", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static string? Invoke(string? input) => (string?)Method.Invoke(null, [input]);

    [Fact]
    public void Null_TraVeNull() => Invoke(null).Should().BeNull();

    [Fact]
    public void ChuoiTrangHoacRong_TraVeNull() => Invoke("   ").Should().BeNull();

    [Fact]
    public void CatKhoangTrangDauCuoi() => Invoke("  Pixel 8  ").Should().Be("Pixel 8");

    [Fact]
    public void BoKyTuDieuKhien() => Invoke("Pixel\t8\n").Should().Be("Pixel8");

    [Theory]
    [InlineData("Pixel​ 8", "Pixel 8")] // U+200B ZERO WIDTH SPACE
    [InlineData("‮evil", "evil")] // U+202E RIGHT-TO-LEFT OVERRIDE — có thể đảo chiều hiển thị tên thiết bị
    [InlineData("iPhone‎15‏Pro", "iPhone15Pro")] // U+200E/U+200F LEFT/RIGHT-TO-LEFT MARK
    public void BoKyTuDinhDangUnicode(string input, string expected) => Invoke(input).Should().Be(expected);

    [Fact]
    public void ChiToanKyTuDinhDangUnicode_TraVeNull() => Invoke("‮​‎").Should().BeNull();

    [Fact]
    public void QuaTramKyTu_BiCatConDungTramKyTu() => Invoke(new string('a', 150)).Should().HaveLength(100);
}
