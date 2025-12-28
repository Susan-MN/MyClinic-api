using System;
using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;
using AutoMapper;

namespace MyClinic.Infrastructure.Servives
{
    public class DoctorService : IDoctorService
    {
        private readonly IGenericRepository<Doctor> _doctorRepository;
        private readonly IAvailabilityRepository _availabilityRepository;
        private readonly IMapper _mapper;

        public DoctorService(
            IGenericRepository<Doctor> doctorRepository,
            IAvailabilityRepository availabilityRepository,
            IMapper mapper)
        {
            _doctorRepository = doctorRepository;
            _availabilityRepository = availabilityRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DoctorResponseDto>> GetAllDoctorsAsync()
        {
            var doctors = await _doctorRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<DoctorResponseDto>>(doctors);
        }

        public async Task<DoctorResponseDto?> GetDoctorByIdAsync(int id)
        {
            var doctor = await _doctorRepository.GetByIdAsync(id);
            return doctor == null ? null : _mapper.Map<DoctorResponseDto>(doctor);
        }

        public async Task<IEnumerable<DoctorResponseDto>> GetApprovedDoctorsAsync()
        {
            try
            {
                var doctors = await _doctorRepository.GetAllAsync();
                var approvedDoctors = doctors.Where(d => d.Status == DoctorStatus.Approved);
                return _mapper.Map<IEnumerable<DoctorResponseDto>>(approvedDoctors);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving approved doctors: {ex.Message}", ex);
            }
        }

        public async Task<DoctorResponseDto?> GetDoctorByKeycloakIdAsync(string keycloakId)
        {
            try
            {
                if (string.IsNullOrEmpty(keycloakId))
                    throw new ArgumentException("KeycloakId cannot be null or empty", nameof(keycloakId));

                var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId);
                return doctor == null ? null : _mapper.Map<DoctorResponseDto>(doctor);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving doctor by KeycloakId: {ex.Message}", ex);
            }
        }

        public async Task<DoctorResponseDto?> UpdateDoctorProfileAsync(string keycloakId, UpdateDoctorRequest request)
        {
            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId);
            if (doctor == null)
                return null;

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.Specialty))
                doctor.Specialty = request.Specialty;

            if (!string.IsNullOrEmpty(request.ImageUrl))
                doctor.ImageUrl = request.ImageUrl;

            if (!string.IsNullOrEmpty(request.Bio))
                doctor.Bio = request.Bio;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
                doctor.PhoneNumber = request.PhoneNumber;
            // Recalculate ProfileComplete 
            var hasRequiredFields =
                !string.IsNullOrWhiteSpace(doctor.Username) &&
                !string.IsNullOrWhiteSpace(doctor.Email) &&
                !string.IsNullOrWhiteSpace(doctor.Specialty) &&
                !string.IsNullOrWhiteSpace(doctor.PhoneNumber) &&
                !string.IsNullOrWhiteSpace(doctor.Bio) &&
                !string.IsNullOrWhiteSpace(doctor.ImageUrl);

            var availability = await _availabilityRepository.GetByDoctorIdAsync(doctor.Id);
            var hasAvailability = availability != null && availability.IsActive;

            doctor.ProfileComplete = hasRequiredFields && hasAvailability;


            _doctorRepository.UpdateAsync(doctor);
            await _doctorRepository.SaveChangesAsync();

            return _mapper.Map<DoctorResponseDto>(doctor);
        }
        public async Task<DoctorResponseDto?> UpdateDoctorStatusAsync(int doctorId, DoctorStatus status)
        {
            var doctor = await _doctorRepository.GetByIdAsync(doctorId);
            if (doctor == null)
                return null;

            //  only approve/reject if profile is complete
            if (status == DoctorStatus.Approved)
            {
                // Check if doctor has minimum required information for approval
                if (string.IsNullOrWhiteSpace(doctor.Specialty))
                {
                    throw new InvalidOperationException("Cannot approve a doctor without a specialty. The doctor must complete their specialty before approval.");
                }
                doctor.Approve();
            }
            else if (status == DoctorStatus.Declined)
            {
                doctor.Decline();
            }

            _doctorRepository.UpdateAsync(doctor);
            await _doctorRepository.SaveChangesAsync();
            
            return _mapper.Map<DoctorResponseDto>(doctor);
        }
    }
}

