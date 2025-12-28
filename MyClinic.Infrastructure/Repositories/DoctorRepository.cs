using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;

namespace MyClinic.Infrastructure.Repositories
{
    public class DoctorRepository : GenericRepository<Doctor>, IDoctorRepository
    {
        private readonly AppDbContext _db;

        public DoctorRepository(AppDbContext db)
            : base(db)
        {
            _db = db;
        }

        public async Task<Doctor?> GetBySpecialityAsync(string Specialty) =>
            await _db.Doctors.FindAsync(Specialty);
    }
}
