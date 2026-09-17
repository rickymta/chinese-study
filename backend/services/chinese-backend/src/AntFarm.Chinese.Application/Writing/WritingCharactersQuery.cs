namespace AntFarm.Chinese.Application.Writing;

/// <summary><c>GET /api/writing/characters?set=&amp;page=&amp;pageSize=</c> (§6.2, R-W6) — <c>class</c> + <c>init</c> có giá trị mặc định (cùng quy ước <c>DictionaryQuery</c>, F6) để tham số thiếu vẫn bind đúng mặc định.</summary>
public sealed class WritingCharactersQuery
{
    /// <summary><c>hsk1</c> | <c>lesson:&lt;slug&gt;</c> | <c>weak</c> | <c>practiced</c> (R-W6).</summary>
    public string Set { get; init; } = "";

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 60;
}
