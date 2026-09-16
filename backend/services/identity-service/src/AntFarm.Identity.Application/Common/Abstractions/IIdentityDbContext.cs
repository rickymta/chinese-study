namespace AntFarm.Identity.Application.Common.Abstractions;

/// <summary>
/// Application chỉ phụ thuộc interface này, không phụ thuộc thẳng EF Core DbContext của
/// Infrastructure (DDD 4 lớp). F0 chỉ có <see cref="SaveChangesAsync"/> — F2 bổ sung
/// <c>DbSet&lt;Account&gt;</c>, <c>DbSet&lt;RefreshToken&gt;</c> khi có entity thật.
/// </summary>
public interface IIdentityDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
