using System.Linq.Expressions;

namespace SEAL.NET.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync();
        Task<T?> GetByIdAsync(Guid id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> Query();
        Task SaveChangesAsync();
    }
}