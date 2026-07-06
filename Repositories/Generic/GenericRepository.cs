using mes_server.Data;
using mes_server.Repositories.Interface.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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


        public async Task CreateAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task DeleteAsync(T entity)
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

        public async Task UpdateAsync(T entity)
        {
           _dbSet.Update(entity);
        }
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

    }
}
