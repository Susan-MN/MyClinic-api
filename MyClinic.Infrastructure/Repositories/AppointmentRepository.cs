using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Interfaces.Repositories;

namespace MyClinic.Infrastructure.Repositories
{
    public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
    {
        private readonly AppDbContext _db;

        public AppointmentRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId)
        {
            return await _db.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetAppointmentsForDoctorAndDateAsync(int doctorId, DateOnly date)
        {
            var targetDate = date.ToDateTime(TimeOnly.MinValue).Date;

            return await _db.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == targetDate)
                .ToListAsync();
        }
    }
}


