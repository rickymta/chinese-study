namespace AntFarm.Cms.Domain.Access;

/// <summary>Vai trò cục bộ (schema `access.roles`, §5.1.1 W1) — danh mục hệ thống, chèn bù bởi <c>AccessSeeder</c> (R-W9/R-W10).</summary>
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
