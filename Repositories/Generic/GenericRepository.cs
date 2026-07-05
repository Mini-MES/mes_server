using mes_server.Data;
using mes_server.Repositories.Interface.Generic;
using Microsoft.EntityFrameworkCore;

namespace mes_server.Repositories.Generic
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly MESDbContext _context;

        protected MESDbContext Context => _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(MESDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }


        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(params object[] keyValues)
        {
            return await _dbSet.FindAsync(keyValues);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Update(T entity)
        {
           _dbSet.Update(entity);
        }
    }
}
