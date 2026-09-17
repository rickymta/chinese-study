using AntFarm.Cms.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AntFarm.Cms.UnitTests.Common;

/// <summary>
/// AddInfrastructure phải ném lỗi RÕ RÀNG bằng tiếng Việt khi thiếu ConnectionStrings:Default,
/// thay vì để Npgsql ném lỗi kết nối mơ hồ lúc runtime (chép khuôn chinese-backend).
/// </summary>
public class AddInfrastructureTests
{
    [Fact]
    public void AddInfrastructure_KhiConnectionStringRong_NemInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "" })
            .Build();

        var act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:Default*");
    }

    [Fact]
    public void AddInfrastructure_KhiThieuKhoaConnectionString_NemInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>();
    }
}
