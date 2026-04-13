using System.Linq.Expressions;
using Platform.Domain.Common;

namespace Platform.Application.Abstractions.Data
{
    public interface IGenericRepository<T> where T : Entity
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
        Task<(List<T> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool isDescending = false,
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        void Update(T entity);
        void Remove(T entity);
        Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate);
        Task<int> DeleteInBatchesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, DateTime>> orderBy, Expression<Func<T, Guid>> keySelector, int batchSize = 100);
        IQueryable<T> GetQueryable();
        Task<int> TotalAsync(Expression<Func<T, bool>> predicate);
    }
}
