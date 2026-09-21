using AntFarm.Identity.Application.Admin;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.UnitTests.Admin;

public class TemporaryPasswordGeneratorTests
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private static readonly char[] ExcludedChars = ['I', 'l', '1', 'O', '0', 'o'];

    [Fact]
    public void Generate_TraVe16KyTu()
    {
        var generator = new TemporaryPasswordGenerator();
        generator.Generate().Should().HaveLength(16);
    }

    [Fact]
    public void Generate_ChiChuaKyTuTrongBang()
    {
        var generator = new TemporaryPasswordGenerator();
        var password = generator.Generate();

        password.All(c => Alphabet.Contains(c)).Should().BeTrue();
        password.Any(c => ExcludedChars.Contains(c)).Should().BeFalse();
    }

    [Fact]
    public void Generate_1000Lan_KhongTrung()
    {
        var generator = new TemporaryPasswordGenerator();
        var passwords = Enumerable.Range(0, 1000).Select(_ => generator.Generate()).ToHashSet();

        passwords.Should().HaveCount(1000);
    }
}
