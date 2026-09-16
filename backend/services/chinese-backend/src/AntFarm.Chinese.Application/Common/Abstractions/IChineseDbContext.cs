namespace AntFarm.Chinese.Application.Common.Abstractions;

/// <summary>
/// Application chỉ phụ thuộc interface này, không phụ thuộc thẳng EF Core DbContext của
/// Infrastructure (DDD 4 lớp). F0 chỉ có <see cref="SaveChangesAsync"/> — F3 bổ sung
/// <c>DbSet&lt;User&gt;</c>, <c>DbSet&lt;Role&gt;</c>... khi có entity thật (schema `access`).
/// </summary>
public interface IChineseDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
