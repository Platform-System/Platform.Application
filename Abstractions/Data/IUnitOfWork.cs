using Microsoft.EntityFrameworkCore.Storage;
using Platform.Domain.Common;

namespace Platform.Application.Abstractions.Data
{
    public interface IUnitOfWork
    {
        IGenericRepository<T> GetRepository<T>() where T : Entity;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        bool HasActiveTransaction { get; }
    }
}
