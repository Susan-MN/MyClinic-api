using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;

namespace MyClinic.Infrastructure.Interfaces.Services
{
    public interface IDoctorService
    {
        Task<IEnumerable<DoctorResponseDto>> GetAllDoctorsAsync();
        Task<IEnumerable<DoctorResponseDto>> GetApprovedDoctorsAsync();
        Task<DoctorResponseDto?> GetDoctorByIdAsync(int id);
        Task<DoctorResponseDto?> GetDoctorByKeycloakIdAsync(string keycloakId);
        Task<DoctorResponseDto?> UpdateDoctorStatusAsync(int doctorId, DoctorStatus status);
        Task<DoctorResponseDto?> UpdateDoctorProfileAsync(string keycloakId, UpdateDoctorRequest request);
    }
}

