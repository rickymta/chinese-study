using AntFarm.Core.Pagination;
using FluentAssertions;
using Xunit;

namespace AntFarm.Shared.UnitTests.Core;

public class PageQueryTests
{
    [Fact]
    public void Normalize_KhiPageNhoHon1_TraVe1()
    {
        var (page, _) = PageQuery.Normalize(0, 20);
        page.Should().Be(1);

        var (pageNull, _) = PageQuery.Normalize(null, 20);
        pageNull.Should().Be(1);

        var (pageAm, _) = PageQuery.Normalize(-5, 20);
        pageAm.Should().Be(1);
    }

    [Fact]
    public void Normalize_KhiPageSizeNhoHon1_TraVeMacDinh20()
    {
        var (_, pageSize) = PageQuery.Normalize(1, 0);
        pageSize.Should().Be(20);

        var (_, pageSizeNull) = PageQuery.Normalize(1, null);
        pageSizeNull.Should().Be(20);
    }

    [Fact]
    public void Normalize_KhiPageSizeVuot100_TraVe100()
    {
        var (_, pageSize) = PageQuery.Normalize(1, 500);
        pageSize.Should().Be(100);
    }
}
