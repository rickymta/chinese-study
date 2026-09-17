namespace AntFarm.Chinese.Application.Lessons;

/// <summary><c>GET /api/lessons/{id}/quiz-attempts?limit=</c> — query string (§6.1); <c>limit</c> 1..20, mặc định 5.</summary>
public sealed class QuizAttemptsQuery
{
    public int Limit { get; init; } = 5;
}
