using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClinic.Infrastructure.Repositories;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;
using MyClinic.Infrastructure.Interfaces.Repositories;

namespace MyClinic.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
            : base(db)
        {
            _db = db;
        }
    }
}
