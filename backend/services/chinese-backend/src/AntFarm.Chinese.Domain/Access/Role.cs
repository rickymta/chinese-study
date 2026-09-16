namespace AntFarm.Chinese.Domain.Access;

/// <summary>Vai trò cục bộ (schema `access.roles`, §5.1.2) — danh mục hệ thống, chèn bù bởi <c>AccessSeeder</c> (R-P3).</summary>
public sealed class Role
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    private Role()
    {
    }

    public static Role Create(string code, string name) => new()
    {
        Id = Guid.CreateVersion7(),
        Code = code,
        Name = name
    };
}
