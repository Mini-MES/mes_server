namespace mes_server.Repositories.Interface.Generic
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(params object[] keyValues);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task SaveChangesAsync();
    }
}
