using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface ISlotConfigRepository : IGenericRepository<SlotConfig>
    {
        Task<SlotConfig?> GetSingletonAsync();
       
    }
}


