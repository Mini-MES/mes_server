namespace mes_server.Repositories.Interface.Generic
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void DeleteById(T entity);
        Task SaveChangesAsync();
    }
}
