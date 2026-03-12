using Microsoft.EntityFrameworkCore;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Infrastructure.Repositories
{
    public class LeaveRepository : GenericRepository<Leave>, ILeaveRepository
    {
        private readonly AppDbContext _db;

        public LeaveRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Leave>> GetByDoctorIdAsync(int doctorId)
        {
            return await _db.Leaves
                .AsNoTracking()
                .Include(l => l.Doctor)
                .Where(l => l.DoctorId == doctorId)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Leave>> GetApprovedLeavesByDoctorIdAsync(int doctorId)
        {
            return await _db.Leaves
                .AsNoTracking()
                .Where(l => l.DoctorId == doctorId && l.IsApproved)
                .OrderByDescending(l => l.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Leave>> GetLeavesByDateRangeAsync(int doctorId, DateOnly startDate, DateOnly endDate)
        {
            return await _db.Leaves
                .AsNoTracking()
                .Where(l => l.DoctorId == doctorId
                    && l.IsApproved
                    && !(l.EndDate < startDate || l.StartDate > endDate))
                .ToListAsync();
        }
        public async Task<IEnumerable<Leave>> GetAllLeavesAsync()
        {
            return await _db.Leaves
                .AsNoTracking()
                .Include(l => l.Doctor)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Leave>> GetPendingLeavesAsync()
        {
            return await _db.Leaves
                .AsNoTracking()
                .Include(l => l.Doctor)
                .Where(l => !l.IsApproved)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }
    }
}