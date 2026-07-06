namespace mes_server.Services.Interface
{
    public interface IGenericService<T> where T : class
    {
        Task<T?> GetByIdAsync(params object[] keyValues);
        Task<IEnumerable<T>> GetAllAsync();
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<T> CreateAsync(T entity);
        Task SaveChangesAsync();
    }
}
