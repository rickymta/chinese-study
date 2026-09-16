using AntFarm.Core.Errors;
using AntFarm.Security.Errors;
using FluentAssertions;
using Xunit;

namespace AntFarm.Shared.UnitTests.Security;

public class ErrorResponseMapperTests
{
    [Fact]
    public void Map_NotFoundException_TraVe404VaMaNotFound()
    {
        var (statusCode, body) = ErrorResponseMapper.Map(new NotFoundException("Không tìm thấy người dùng."));

        statusCode.Should().Be(404);
        body.Code.Should().Be("NOT_FOUND");
        body.Error.Should().Be("Không tìm thấy người dùng.");
    }

    [Fact]
    public void Map_BusinessRuleException_LastAdmin_TraVe422()
    {
        var (statusCode, body) = ErrorResponseMapper.Map(
            new BusinessRuleException("LAST_ADMIN", "Không thể gỡ vai trò của quản trị viên cuối cùng."));

        statusCode.Should().Be(422);
        body.Code.Should().Be("LAST_ADMIN");
    }

    [Fact]
    public void Map_ExceptionLa_TraVe500VaKhongLoMessageGoc()
    {
        var (statusCode, body) = ErrorResponseMapper.Map(
            new InvalidOperationException("Connection string chứa mật khẩu bí mật."));

        statusCode.Should().Be(500);
        body.Code.Should().Be("INTERNAL_ERROR");
        body.Error.Should().Be("Đã xảy ra lỗi nội bộ.");
        body.Error.Should().NotContain("mật khẩu");
    }
}
