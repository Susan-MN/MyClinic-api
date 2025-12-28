using MyClinic.Application.DTO;

namespace MyClinic.Infrastructure.Interfaces.Services
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentResponseDto>> GetAppointmentsByDoctorIdAsync(int doctorId);
        Task<IEnumerable<AppointmentResponseDto>> GetAppointmentsByDoctorIdAndDateAsync(int doctorId, DateOnly date);
        Task<IEnumerable<AppointmentResponseDto>> GetAppointmentsByKeycloakIdAsync(string keycloakId);
        Task<AppointmentResponseDto> BookAppointmentAsync(string patientKeycloakId, CreateAppointmentRequest request);
    }
}

