using System;
using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Interfaces.Repositories
{
    public interface IAppointmentRepository : IGenericRepository<Appointment>
    {
        Task<IEnumerable<Appointment>> GetByDoctorIdAsync(int doctorId);
        Task<IEnumerable<Appointment>> GetAppointmentsForDoctorAndDateAsync(int doctorId, DateOnly date);
    }
}


