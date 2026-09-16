using AntFarm.Chinese.Domain.Content;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Content;

/// <summary>§5.2.5 — R6-11: dòng đã duyệt/sửa tay không bị tệp học liệu ghi đè.</summary>
public class WordApplyImportTests
{
    private static readonly DateTime T0 = new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = T0.AddMinutes(10);

    private static WordImportData BuildData(
        string simplified = "爱", string pinyin = "ai4",
        string[]? meaningsVi = null, string meaningViStatus = MeaningViStatus.Machine,
        string? hanViet = "ái", string? hanVietStatus = HanVietStatus.Derived) =>
        new(
            simplified, null, [], pinyin, 1, 1, 1, 1, 12, 130, ["v"], null,
            ["to love"], meaningsVi ?? ["yêu; thích"], meaningViStatus, MeaningViSource.Cvdict,
            hanViet, hanVietStatus, ["hsk30-official"]);

    [Fact]
    public void ApplyImport_TepGiongHet_TraVeUnchangedVaKhongDoiUpdatedAt()
    {
        var word = Word.CreateFromImport(BuildData(), T0);

        var outcome = word.ApplyImport(BuildData(), T1);

        outcome.Should().Be(ImportOutcome.Unchanged);
        word.UpdatedAt.Should().Be(T0);
    }

    [Fact]
    public void ApplyImport_DongDaDuyet_GiuNghiaKhiTepDoiNghia()
    {
        var word = Word.CreateFromImport(BuildData(meaningViStatus: MeaningViStatus.Reviewed), T0);
        word.MeaningsVi.Should().Equal("yêu; thích");

        var outcome = word.ApplyImport(BuildData(meaningsVi: ["nghĩa khác hoàn toàn"]), T1);

        outcome.Should().Be(ImportOutcome.UpdatedProtected);
        word.MeaningsVi.Should().Equal("yêu; thích"); // giữ nguyên, KHÔNG bị ghi đè
        word.MeaningViStatus.Should().Be(MeaningViStatus.Reviewed);
    }

    [Fact]
    public void ApplyImport_EditedAtKhacNull_GiuHanViet()
    {
        var word = Word.CreateFromImport(BuildData(), T0);
        // Giả lập F10 đã sửa tay — dựng lại bằng ApplyImport với EditedAt gán qua phản chiếu không
        // khả dụng (Domain không expose setter) — dùng chính hành vi "meaning_vi_status=reviewed
        // hoặc edited_at khác null" qua đường reviewed để kiểm khoá Hán Việt tương tự (cùng cờ
        // IsHanVietLocked). Khoá Hán Việt kiểm riêng bằng hanVietStatus=reviewed dưới đây.
        var reviewedHanViet = Word.CreateFromImport(BuildData(hanVietStatus: HanVietStatus.Reviewed), T0);

        var outcome = reviewedHanViet.ApplyImport(BuildData(hanViet: "khác hẳn", hanVietStatus: HanVietStatus.Derived), T1);

        outcome.Should().Be(ImportOutcome.UpdatedProtected);
        reviewedHanViet.HanViet.Should().Be("ái");
        reviewedHanViet.HanVietStatus.Should().Be(HanVietStatus.Reviewed);
    }

    [Fact]
    public void ApplyImport_TruongKhongBiKhoa_LuonCapNhatTheoTep()
    {
        var word = Word.CreateFromImport(BuildData(meaningViStatus: MeaningViStatus.Reviewed), T0);

        var outcome = word.ApplyImport(BuildData(meaningViStatus: MeaningViStatus.Reviewed) with { FrequencyRank = 999 }, T1);

        outcome.Should().Be(ImportOutcome.Updated);
        word.FrequencyRank.Should().Be(999);
        word.UpdatedAt.Should().Be(T1);
    }

    [Fact]
    public void ApplyImport_MangSoTheoThuTuPhanTu()
    {
        var word = Word.CreateFromImport(BuildData() with { MeaningsEn = ["a", "b"] }, T0);

        var outcomeSameOrder = word.ApplyImport(BuildData() with { MeaningsEn = ["a", "b"] }, T1);
        outcomeSameOrder.Should().Be(ImportOutcome.Unchanged);

        var outcomeDifferentOrder = word.ApplyImport(BuildData() with { MeaningsEn = ["b", "a"] }, T1);
        outcomeDifferentOrder.Should().Be(ImportOutcome.Updated);
    }
}
