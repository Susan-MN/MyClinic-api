using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyClinic.Application.Interfaces.Repositories;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Data;

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
