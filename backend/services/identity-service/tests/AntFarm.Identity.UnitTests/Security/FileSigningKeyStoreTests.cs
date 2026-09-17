using AntFarm.Identity.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace AntFarm.Identity.UnitTests.Security;

/// <summary>R-A13 — Development: thư mục trống tự sinh khoá; môi trường khác: ném lỗi rõ ràng.</summary>
public class FileSigningKeyStoreTests
{
    [Fact]
    public void Constructor_ThuMucTrongODevelopment_TuSinhKhoa()
    {
        var dir = Directory.CreateTempSubdirectory("antfarm-fsks-test-").FullName;
        try
        {
            var store = new FileSigningKeyStore(dir, activeKeyId: null, isDevelopment: true);

            Directory.EnumerateFiles(dir, "*.pem").Should().HaveCount(1);
            store.GetPublicKeys().Should().HaveCount(1);
            store.GetActiveSigningCredentials().Key.KeyId.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_ThuMucTrongKhongPhaiDevelopment_NemLoiRoRang()
    {
        var dir = Directory.CreateTempSubdirectory("antfarm-fsks-test-").FullName;
        try
        {
            var act = () => new FileSigningKeyStore(dir, activeKeyId: null, isDevelopment: false);

            act.Should().Throw<InvalidOperationException>().WithMessage("*Không tìm thấy khoá ký*");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void GetPublicJsonWebKeys_TraVeDungKtyUseAlg()
    {
        var dir = Directory.CreateTempSubdirectory("antfarm-fsks-test-").FullName;
        try
        {
            var store = new FileSigningKeyStore(dir, activeKeyId: null, isDevelopment: true);
            var jwks = store.GetPublicJsonWebKeys();

            jwks.Should().ContainSingle();
            jwks[0].Kty.Should().Be("RSA");
            jwks[0].Use.Should().Be("sig");
            jwks[0].Alg.Should().Be("RS256");
            jwks[0].Kid.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
