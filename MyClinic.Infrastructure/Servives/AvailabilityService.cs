using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;
using System.Text.Json;
using System.Globalization;

namespace MyClinic.Infrastructure.Servives
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IAvailabilityRepository _availabilityRepository;
        private readonly IGenericRepository<Doctor> _doctorRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly ISlotConfigRepository _slotConfigRepository;

        public AvailabilityService(
            IAvailabilityRepository availabilityRepository,
            IGenericRepository<Doctor> doctorRepository,
            IAppointmentRepository appointmentRepository,
            ISlotConfigRepository slotConfigRepository)
        {
            _availabilityRepository = availabilityRepository;
            _doctorRepository = doctorRepository;
            _appointmentRepository = appointmentRepository;
            _slotConfigRepository = slotConfigRepository;
        }

        public async Task<AvailabilityResponseDto?> GetAvailabilityByDoctorIdAsync(int doctorId)
        {
            var availability = await _availabilityRepository.GetByDoctorIdAsync(doctorId);
            
            if (availability == null)
                return null;

            return MapToDto(availability);
        }

        public async Task<AvailabilityResponseDto?> GetAvailabilityByKeycloakIdAsync(string keycloakId)
        {
            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId);
            if (doctor == null)
                return null;

            return await GetAvailabilityByDoctorIdAsync(doctor.Id);
        }

        public async Task<IEnumerable<SlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date)
        {
            var availability = await _availabilityRepository.GetByDoctorIdAsync(doctorId);
            if (availability == null || !availability.IsActive)
                return Enumerable.Empty<SlotDto>();

            // Check if doctor works on this weekday
            var workingDays = JsonSerializer.Deserialize<List<string>>(availability.WorkingDaysJson) ?? new();
            var dayName = date.ToString("dddd", CultureInfo.InvariantCulture).ToLowerInvariant();
            if (!workingDays.Any(d => d.Equals(dayName, StringComparison.OrdinalIgnoreCase)))
                return Enumerable.Empty<SlotDto>();

            // Slot duration (use doctor's configured value)
            var duration = availability.SlotDuration;

            // Parse start/end times
            var start = TimeOnly.Parse(availability.StartTime);
            var end = TimeOnly.Parse(availability.EndTime);

            // Fetch existing appointments for the doctor on that date
            var bookedSlots = await _appointmentRepository
                .GetAppointmentsForDoctorAndDateAsync(doctorId, date);

            var slots = new List<SlotDto>();
            var cursor = start;

            while (cursor.AddMinutes(duration) <= end)
            {
                var slotStart = cursor;
                var slotEnd = cursor.AddMinutes(duration);

                var overlaps = bookedSlots.Any(a =>
                {
                    var apptStart = TimeOnly.FromTimeSpan(a.StartTime);
                    var apptEnd = TimeOnly.FromTimeSpan(a.EndTime);
                    return !(slotEnd <= apptStart || slotStart >= apptEnd);
                });

                slots.Add(new SlotDto
                {
                    StartTime = slotStart.ToString("HH:mm"),
                    EndTime = slotEnd.ToString("HH:mm"),
                    IsAvailable = !overlaps
                });

                cursor = slotEnd;
            }

            return slots;
        }
        private AvailabilityResponseDto MapToDto(Availability availability)
        {
            var dto = new AvailabilityResponseDto
            {
                Id = $"avail-{availability.Id}",
                DoctorId = availability.DoctorId,
                StartTime = availability.StartTime,
                EndTime = availability.EndTime,
                SlotDuration = availability.SlotDuration,
                IsActive = availability.IsActive
            };

            // Parse JSON array to List<string>
            try
            {
                dto.WorkingDays = JsonSerializer.Deserialize<List<string>>(availability.WorkingDaysJson) ?? new List<string>();
            }
            catch
            {
                dto.WorkingDays = new List<string>();
            }

            return dto;
        }
        public async Task<AvailabilityResponseDto?> UpsertAvailabilityAsync(string keycloakId,UpdateAvailabilityRequest request)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("KeycloakId cannot be empty.", nameof(keycloakId));

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId)
                ?? throw new InvalidOperationException("Doctor not found for the current user.");

            var availability = await _availabilityRepository.GetByDoctorIdAsync(doctor.Id);
            var workingDaysJson = JsonSerializer.Serialize(request.WorkingDays ?? new List<string>());        

            if (availability == null)
            {
                availability = new Availability
                {
                    DoctorId = doctor.Id,
                    WorkingDaysJson = workingDaysJson,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    SlotDuration = request.SlotDuration,
                    IsActive = request.AcceptBookings
                };

                await _availabilityRepository.AddAsync(availability);
            }
            else
            {
                availability.WorkingDaysJson = workingDaysJson;
                availability.StartTime = request.StartTime;
                availability.EndTime = request.EndTime;
                availability.SlotDuration = request.SlotDuration;
                availability.IsActive = request.AcceptBookings;

                _availabilityRepository.UpdateAsync(availability);
            }

            await _availabilityRepository.SaveChangesAsync();

            return MapToDto(availability);
        }
    }
}

