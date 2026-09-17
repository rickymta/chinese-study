using System.Security.Cryptography;

namespace AntFarm.Identity.Application.Admin;

/// <summary>Interface tách riêng để test được (đếm ký tự, không trùng nhiều lần liên tiếp) mà không phụ thuộc <see cref="RandomNumberGenerator"/> thật.</summary>
public interface ITemporaryPasswordGenerator
{
    string Generate();
}

/// <summary>
/// Mật khẩu tạm 16 ký tự do admin cấp khi đặt lại mật khẩu (§5.2.9) — bỏ các ký tự dễ nhầm lẫn
/// khi đọc qua điện thoại/email (I, l, 1, O, 0, o).
/// </summary>
public sealed class TemporaryPasswordGenerator : ITemporaryPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private const int Length = 16;

    public string Generate() => new(RandomNumberGenerator.GetItems<char>(Alphabet, Length));
}
