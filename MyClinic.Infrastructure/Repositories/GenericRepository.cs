using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Data;

namespace MyClinic.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T>
        where T : class
    {
        private readonly AppDbContext _db;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext db)
        {
            _db = db;
            _dbSet = db.Set<T>();
        }

        public async Task AddAsync(T enitity) => await _dbSet.AddAsync(enitity);

        public void DeleteAsync(T entity) => _dbSet.Remove(entity);

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task SaveChangesAsync() => await _db.SaveChangesAsync();

        public void UpdateAsync(T entity) => _dbSet.Update(entity);

        public async Task<T?> GetByKeycloakIdAsync(string keycloakId)
        {
            if (string.IsNullOrEmpty(keycloakId))
                return null;

            try
            {
                return await _dbSet.FirstOrDefaultAsync(e =>
                    EF.Property<string?>(e, "KeycloakId") == keycloakId
                );
            }
            catch (Exception ex)
            {
                
                throw new InvalidOperationException(
                    $"Error querying {typeof(T).Name} by KeycloakId. " +
                    $"This entity may not have a KeycloakId property configured. " +
                    $"Original error: {ex.Message}", ex);
            }
        }
    }
}
