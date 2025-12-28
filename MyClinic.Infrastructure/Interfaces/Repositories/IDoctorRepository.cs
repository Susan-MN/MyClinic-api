using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface IDoctorRepository
    {
        Task<Doctor?> GetBySpecialityAsync(string Specialty);
    }
}
