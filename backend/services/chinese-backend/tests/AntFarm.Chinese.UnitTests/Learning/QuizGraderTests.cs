using AntFarm.Chinese.Domain.Learning;
using FluentAssertions;
using Xunit;

namespace AntFarm.Chinese.UnitTests.Learning;

/// <summary>§5.2.1.6 — R-LS3: <c>correct*100 >= 80*total</c> (không làm tròn); <c>scorePercent</c> = floor(correct*100/total).</summary>
public class QuizGraderTests
{
    private static List<LessonQuizQuestionSnapshot> Questions(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => new LessonQuizQuestionSnapshot(Guid.NewGuid(), "a"))];

    private static Dictionary<Guid, string> Answers(IReadOnlyList<LessonQuizQuestionSnapshot> questions, int correctCount)
    {
        var answers = new Dictionary<Guid, string>();
        for (var i = 0; i < questions.Count; i++)
            answers[questions[i].Id] = i < correctCount ? questions[i].CorrectOptionId : "b";
        return answers;
    }

    [Fact]
    public void BonTrenNam_Dat80PhanTram()
    {
        var questions = Questions(5);
        var grade = QuizGrader.Grade(questions, Answers(questions, 4));

        grade.Total.Should().Be(5);
        grade.Correct.Should().Be(4);
        grade.ScorePercent.Should().Be(80);
        grade.Passed.Should().BeTrue();
    }

    [Fact]
    public void BayTrenChin_KhongLamTron_KhongDat()
    {
        var questions = Questions(9);
        var grade = QuizGrader.Grade(questions, Answers(questions, 7));

        grade.ScorePercent.Should().Be(77); // floor(700/9) = 77, không làm tròn lên 78
        grade.Passed.Should().BeFalse(); // 700 < 720
    }

    [Fact]
    public void TamTrenMuoi_Dat()
    {
        var questions = Questions(10);
        var grade = QuizGrader.Grade(questions, Answers(questions, 8));

        grade.ScorePercent.Should().Be(80);
        grade.Passed.Should().BeTrue();
    }

    [Fact]
    public void KhongDungCauNao_DiemKhong()
    {
        var questions = Questions(5);
        var grade = QuizGrader.Grade(questions, Answers(questions, 0));

        grade.ScorePercent.Should().Be(0);
        grade.Passed.Should().BeFalse();
    }

    [Fact]
    public void DungHetCacCau_TramPhanTram()
    {
        var questions = Questions(5);
        var grade = QuizGrader.Grade(questions, Answers(questions, 5));

        grade.ScorePercent.Should().Be(100);
        grade.Passed.Should().BeTrue();
    }

    [Fact]
    public void TapCauHoiLech_Nem()
    {
        var questions = Questions(3);
        var answers = new Dictionary<Guid, string> { [Guid.NewGuid()] = "a" }; // câu lạ, không thuộc bộ câu hiện có

        var act = () => QuizGrader.Grade(questions, answers);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ThieuMotCauSoVoiBoCauHienCo_Nem()
    {
        var questions = Questions(3);
        var answers = Answers(questions, 3);
        answers.Remove(questions[0].Id); // chỉ trả lời 2/3 câu

        var act = () => QuizGrader.Grade(questions, answers);

        act.Should().Throw<ArgumentException>();
    }
}
