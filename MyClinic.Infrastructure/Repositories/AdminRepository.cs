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
    public class AdminRepository : GenericRepository<Admin>, IAdminRepository
    {
        private readonly AppDbContext _db;

        public AdminRepository(AppDbContext db)
            : base(db)
        {
            _db = db;
        }
    }
}
