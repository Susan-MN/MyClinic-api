using System;
using MyClinic.Application.DTO;

namespace MyClinic.Infrastructure.Interfaces.Services
{
    public interface IAvailabilityService
    {
        Task<AvailabilityResponseDto?> GetAvailabilityByDoctorIdAsync(int doctorId);
        Task<AvailabilityResponseDto?> GetAvailabilityByKeycloakIdAsync(string keycloakId);
        Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date);
        Task<AvailabilityResponseDto?> UpsertAvailabilityAsync(string keycloakId, UpdateAvailabilityRequest request);
        Task<bool> DeleteAvailabilityAsync(string keycloakId);
        Task<AvailabilityResponseDto?> UpsertAvailabilityDaysAsync(string keycloakId, List<AvailabilityDayRequest> days);
        Task<IEnumerable<AvailabilityDayResponseDto>> GetAvailabilityDaysByKeycloakIdAsync(string keycloakId);
    }
}

