using System.Text.Json;
using AntFarm.Chinese.Application.Lessons;
using AntFarm.Chinese.Domain.Lessons;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Lessons;

/// <summary>§5.2.1.6 — ≥ 15 ca kiểm <c>LessonContentValidator</c> (khối, câu hỏi, cú pháp chữ Hán nội dòng).</summary>
public class LessonContentValidatorTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    // ---- khối text ----

    [Fact]
    public void Text_ParagraphRong_Loi()
    {
        var payload = Parse("""{ "paragraphs": [""] }""");
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Text, payload);
        problems.Should().NotBeEmpty();
    }

    [Fact]
    public void Text_MotDoanHopLe_KhongLoi()
    {
        var payload = Parse("""{ "paragraphs": ["Xin chào các bạn."] }""");
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Text, payload);
        problems.Should().BeEmpty();
    }

    // ---- khối dialogue ----

    [Fact]
    public void Dialogue_PinyinThieuAmTiet_Loi()
    {
        // 你好 (2 chữ Hán) nhưng pinyin chỉ có 1 âm tiết "ni3" — số âm tiết khác số chữ Hán.
        var payload = Parse("""
            { "lines": [
                { "speaker": "Lan", "hanzi": "你好", "pinyin": "ni3", "vi": "Chào bạn" },
                { "speaker": "Minh", "hanzi": "你好", "pinyin": "ni3 hao3", "vi": "Chào bạn" }
            ] }
            """);
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Dialogue, payload);
        problems.Should().Contain(p => p.Message.Contains("số âm tiết"));
    }

    [Fact]
    public void Dialogue_PinyinCoDauCau_HopLe()
    {
        var payload = Parse("""
            { "lines": [
                { "speaker": "Lan", "hanzi": "你好！", "pinyin": "Ni3 hao3!", "vi": "Chào bạn!" },
                { "speaker": "Minh", "hanzi": "你好！", "pinyin": "Ni3 hao3!", "vi": "Chào bạn!" }
            ] }
            """);
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Dialogue, payload);
        problems.Should().BeEmpty();
    }

    [Fact]
    public void Dialogue_UVietLv4_HopLe()
    {
        var payload = Parse("""
            { "lines": [
                { "speaker": "Lan", "hanzi": "女", "pinyin": "lv3", "vi": "nữ" },
                { "speaker": "Minh", "hanzi": "绿", "pinyin": "lv4", "vi": "màu xanh lá" }
            ] }
            """);
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Dialogue, payload);
        problems.Should().BeEmpty();
    }

    [Fact]
    public void Dialogue_ItHonHaiDong_Loi()
    {
        var payload = Parse("""{ "lines": [ { "speaker": "Lan", "hanzi": "你好", "pinyin": "ni3 hao3", "vi": "Chào bạn" } ] }""");
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Dialogue, payload);
        problems.Should().NotBeEmpty();
    }

    // ---- khối grammar ----

    [Fact]
    public void Grammar_KhongViDu_Loi()
    {
        var payload = Parse("""{ "title": "Mẫu câu", "explanation": "Giải thích.", "examples": [] }""");
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Grammar, payload);
        problems.Should().Contain(p => p.Path.Contains("examples"));
    }

    [Fact]
    public void Grammar_CoViDuVaPattern_HopLe()
    {
        var payload = Parse("""
            { "title": "Câu hỏi với 吗", "pattern": "Câu trần thuật + [[吗|ma5]]？",
              "explanation": "Thêm [[吗|ma5]] vào cuối câu.",
              "examples": [ { "hanzi": "你好吗？", "pinyin": "Ni3 hao3 ma5?", "vi": "Bạn khoẻ không?" } ] }
            """);
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Grammar, payload);
        problems.Should().BeEmpty();
    }

    // ---- khối tip ----

    [Fact]
    public void Tip_VariantLa_Loi()
    {
        var payload = Parse("""{ "text": "Mẹo nhỏ.", "variant": "khong-hop-le" }""");
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Tip, payload);
        problems.Should().Contain(p => p.Path.Contains("variant"));
    }

    [Fact]
    public void Tip_VariantHopLe_KhongLoi()
    {
        var payload = Parse("""{ "text": "Mẹo phát âm.", "variant": "pronunciation" }""");
        var problems = LessonContentValidator.ValidateBlock(LessonBlockTypes.Tip, payload);
        problems.Should().BeEmpty();
    }

    // ---- câu hỏi quiz ----

    [Fact]
    public void Question_ListenChoiceThieuAudioText_Loi()
    {
        var input = new QuestionValidationInput(
            QuizQuestionTypes.ListenChoice, "Nghe và chọn nghĩa đúng", "vi", null, null,
            [new QuizOption("a", "cảm ơn", "vi"), new QuizOption("b", "xin lỗi", "vi")], "a", "Giải thích.");
        var problems = LessonContentValidator.ValidateQuestion(input);
        problems.Should().Contain(p => p.Path.Contains("audioText"));
    }

    [Fact]
    public void Question_CorrectOptionIdKhongThuocLuaChon_Loi()
    {
        var input = new QuestionValidationInput(
            QuizQuestionTypes.SingleChoice, "你好吗？", "zh", "Ni3 hao3 ma5?", null,
            [new QuizOption("a", "Bạn khoẻ không?", "vi"), new QuizOption("b", "Bạn là ai?", "vi")], "z", "Giải thích.");
        var problems = LessonContentValidator.ValidateQuestion(input);
        problems.Should().Contain(p => p.Path.Contains("correctOptionId"));
    }

    [Fact]
    public void Question_IdLuaChonTrung_Loi()
    {
        var input = new QuestionValidationInput(
            QuizQuestionTypes.SingleChoice, "你好吗？", "zh", "Ni3 hao3 ma5?", null,
            [new QuizOption("a", "Một", "vi"), new QuizOption("a", "Hai", "vi")], "a", "Giải thích.");
        var problems = LessonContentValidator.ValidateQuestion(input);
        problems.Should().Contain(p => p.Message.Contains("trùng"));
    }

    [Fact]
    public void Question_LangPinyinSaiDinhDang_Loi()
    {
        var input = new QuestionValidationInput(
            QuizQuestionTypes.SingleChoice, "你好", "zh", "Ni3 hao3", null,
            [new QuizOption("a", "ni-hao", "pinyin"), new QuizOption("b", "zai4 jian4", "pinyin")], "b", "Giải thích.");
        var problems = LessonContentValidator.ValidateQuestion(input);
        problems.Should().Contain(p => p.Message.Contains("pinyin"));
    }

    [Fact]
    public void Question_HopLeDayDu_KhongLoi()
    {
        var input = new QuestionValidationInput(
            QuizQuestionTypes.ListenChoice, "Nghe và chọn nghĩa đúng", "vi", null, "谢谢",
            [new QuizOption("a", "xin lỗi", "vi"), new QuizOption("b", "cảm ơn", "vi")], "b", "谢谢 nghĩa là cảm ơn.");
        var problems = LessonContentValidator.ValidateQuestion(input);
        problems.Should().BeEmpty();
    }

    // ---- cú pháp chữ Hán nội dòng ----

    [Fact]
    public void InlineToken_HopLe_KhongLoi()
    {
        var problems = LessonContentValidator.ValidateInlineTokens("path", "Câu chào phổ biến là [[你好|ni3 hao3]].");
        problems.Should().BeEmpty();
    }

    [Fact]
    public void InlineToken_SoAmTietLech_Loi()
    {
        var problems = LessonContentValidator.ValidateInlineTokens("path", "Câu chào phổ biến là [[你好|ni3]].");
        problems.Should().NotBeEmpty();
    }

    [Fact]
    public void InlineToken_KhongPhaiChuHan_Loi()
    {
        var problems = LessonContentValidator.ValidateInlineTokens("path", "Token hỏng [[abc|a1]].");
        problems.Should().NotBeEmpty();
    }

    [Fact]
    public void InlineToken_ThieuDongNgoac_Loi()
    {
        var problems = LessonContentValidator.ValidateInlineTokens("path", "Token hỏng [[你好|ni3 hao3.");
        problems.Should().NotBeEmpty();
    }

    // ---- glossary ----

    [Fact]
    public void Glossary_HopLe_KhongLoi()
    {
        var problems = LessonContentValidator.ValidateGlossary([new GlossaryItem("越南", "Yue4 nan2", "Việt Nam")]);
        problems.Should().BeEmpty();
    }

    [Fact]
    public void Glossary_HanzeLanChuLatin_Loi()
    {
        var problems = LessonContentValidator.ValidateGlossary([new GlossaryItem("abc", "a1 b2 c3", "không hợp lệ")]);
        problems.Should().NotBeEmpty();
    }
}
