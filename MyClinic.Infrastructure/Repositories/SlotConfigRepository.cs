using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Interfaces.Repositories;

namespace MyClinic.Infrastructure.Repositories
{
    public class SlotConfigRepository : GenericRepository<SlotConfig>, ISlotConfigRepository
    {
        private readonly AppDbContext _db;

        public SlotConfigRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<SlotConfig?> GetSingletonAsync()
        {
            return await _db.SlotConfigs
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();
        }
    }
}


