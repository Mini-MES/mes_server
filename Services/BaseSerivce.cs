using mes_server.Repositories.Interface.Generic;
using mes_server.Services.Interface;

namespace mes_server.Services
{
    public class BaseSerivce<T> : IGenericService<T> where T : class
    {
        private readonly IGenericRepository<T> _repository;

        public BaseSerivce(IGenericRepository<T> repository)
        {
            _repository = repository;
        }

        public async Task<T> CreateAsync(T entity)
        {
            await _repository.CreateAsync(entity);
            await _repository.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(T entity)
        {
            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<T?> GetByIdAsync(params object[] keyValues)
        {
            return await _repository.GetByIdAsync(keyValues);
        }


        public async Task UpdateAsync(T entity)
        {
            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
        }
    }
}
