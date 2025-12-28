using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface IGenericRepository<T>
        where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByKeycloakIdAsync(string keycloakId);
        Task AddAsync(T enitity);
        void UpdateAsync(T entity);
        void DeleteAsync(T entity);
        Task SaveChangesAsync();
    }
}
