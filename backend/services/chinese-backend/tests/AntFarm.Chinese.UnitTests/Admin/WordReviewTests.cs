using AntFarm.Chinese.Domain.Content;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Admin;

/// <summary>§5.2.3 "Duyệt từ" — R-CA9: <see cref="Word.ApplyReview"/> quyết <c>meaning_vi_source</c> theo nội dung có đổi hay không; <c>hanViet</c> rỗng ⇒ <c>null</c> (kéo theo <c>hanVietStatus</c> cũng <c>null</c>).</summary>
public class WordReviewTests
{
    private static readonly DateTime T0 = new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = T0.AddMinutes(10);
    private static readonly Guid AdminId = Guid.NewGuid();

    private static Word BuildWord(
        string[]? meaningsVi = null, string meaningViStatus = MeaningViStatus.Machine, string meaningViSource = MeaningViSource.Cvdict,
        string? hanViet = "ái", string? hanVietStatus = HanVietStatus.Derived)
    {
        var data = new WordImportData(
            "爱", null, [], "ai4", 1, 1, 1, 1, 12, 130, ["v"], null,
            ["to love"], meaningsVi ?? ["yêu; thích"], meaningViStatus, meaningViSource,
            hanViet, hanVietStatus, ["hsk30-official"]);
        return Word.CreateFromImport(data, T0);
    }

    [Fact]
    public void ApplyReview_DoiNoiDungNghia_ChuyenSangManual()
    {
        var word = BuildWord();

        word.ApplyReview(["nghĩa mới do admin sửa"], MeaningViStatus.Reviewed, word.HanViet, word.HanVietStatus!, AdminId, T1);

        word.MeaningsVi.Should().Equal("nghĩa mới do admin sửa");
        word.MeaningViSource.Should().Be(MeaningViSource.Manual);
        word.MeaningViStatus.Should().Be(MeaningViStatus.Reviewed);
        word.EditedAt.Should().Be(T1);
        word.EditedBy.Should().Be(AdminId);
    }

    [Fact]
    public void ApplyReview_GiuNguyenNoiDung_ChiDoiStatus_GiuNguonCu()
    {
        var word = BuildWord(meaningViSource: MeaningViSource.Cvdict);

        // Cùng nội dung (thứ tự + giá trị y hệt) — chỉ đổi trạng thái sang reviewed (duyệt hàng loạt).
        word.ApplyReview(word.MeaningsVi, MeaningViStatus.Reviewed, word.HanViet, word.HanVietStatus!, AdminId, T1);

        word.MeaningsVi.Should().Equal("yêu; thích");
        word.MeaningViSource.Should().Be(MeaningViSource.Cvdict); // KHÔNG bị đổi thành manual
        word.MeaningViStatus.Should().Be(MeaningViStatus.Reviewed);
    }

    [Fact]
    public void ApplyReview_HanVietRong_ThanhNull_KeoTheoStatusCungNull()
    {
        var word = BuildWord(hanViet: "ái", hanVietStatus: HanVietStatus.Derived);

        word.ApplyReview(word.MeaningsVi, word.MeaningViStatus, "", HanVietStatus.Reviewed, AdminId, T1);

        word.HanViet.Should().BeNull();
        word.HanVietStatus.Should().BeNull();
    }

    [Fact]
    public void ApplyReview_HanVietKhoangTrang_ThanhNull()
    {
        var word = BuildWord();

        word.ApplyReview(word.MeaningsVi, word.MeaningViStatus, "   ", HanVietStatus.Reviewed, AdminId, T1);

        word.HanViet.Should().BeNull();
        word.HanVietStatus.Should().BeNull();
    }

    [Fact]
    public void ApplyReview_DoiHanViet_GiuNghiaKhongDoi_KhongThanhManual()
    {
        var word = BuildWord(meaningViSource: MeaningViSource.Machine);

        word.ApplyReview(word.MeaningsVi, word.MeaningViStatus, "khác hẳn", HanVietStatus.Reviewed, AdminId, T1);

        word.HanViet.Should().Be("khác hẳn");
        word.HanVietStatus.Should().Be(HanVietStatus.Reviewed);
        word.MeaningViSource.Should().Be(MeaningViSource.Machine); // nghĩa không đổi ⇒ nguồn nghĩa giữ nguyên
    }

    [Fact]
    public void ApplyReview_LuonTinhLaiKhoaTimKiem()
    {
        var word = BuildWord();

        word.ApplyReview(["nghĩa abc"], MeaningViStatus.Reviewed, "ái", HanVietStatus.Reviewed, AdminId, T1);

        // RecomputeSearchKeys chạy lại bên trong ApplyReview — search_vi_plain phải phản ánh nghĩa MỚI.
        word.SearchViPlain.Should().Contain("abc");
    }
}
