using AntFarm.Identity.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AntFarm.Identity.Application.Common.Abstractions;

/// <summary>
/// Application chỉ phụ thuộc interface này, không phụ thuộc thẳng EF Core DbContext của
/// Infrastructure (DDD 4 lớp). <see cref="BeginTransactionAsync"/> phục vụ luồng xoay refresh
/// token (R-A5/R-A6) cần <c>SELECT ... FOR UPDATE</c> trong một transaction rõ ràng.
/// </summary>
public interface IIdentityDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
