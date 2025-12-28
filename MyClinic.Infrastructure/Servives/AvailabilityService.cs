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
        private readonly IAvailabilityDayRepository _availabilityDayRepository;
        private readonly IAvailabilityExceptionRepository _exceptionRepository;
        private readonly IGenericRepository<Doctor> _doctorRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly ISlotConfigRepository _slotConfigRepository;

        public AvailabilityService(
            IAvailabilityDayRepository availabilityDayRepository,
            IAvailabilityExceptionRepository exceptionRepository,
            IGenericRepository<Doctor> doctorRepository,
            IAppointmentRepository appointmentRepository,
            ISlotConfigRepository slotConfigRepository)
        {
            _availabilityDayRepository = availabilityDayRepository;
            _exceptionRepository = exceptionRepository;
            _doctorRepository = doctorRepository;
            _appointmentRepository = appointmentRepository;
            _slotConfigRepository = slotConfigRepository;
        }

        public async Task<AvailabilityResponseDto?> GetAvailabilityByDoctorIdAsync(int doctorId)
        {
            var availabilityDays = await _availabilityDayRepository.GetByDoctorIdAsync(doctorId);
            
            if (!availabilityDays.Any())
                return null;

            // Get active days for the response, but use all days for the structure
            var activeDays = availabilityDays.Where(ad => ad.IsActive).ToList();
            var firstDay = availabilityDays.First();
            var workingDays = availabilityDays
                .Select(ad => GetDayName(ad.DayOfWeek))
                .ToList();

            return new AvailabilityResponseDto
            {
                Id = $"avail-{doctorId}",
                DoctorId = doctorId,
                WorkingDays = workingDays,
                StartTime = firstDay.StartTime.ToString(@"hh\:mm"),
                EndTime = firstDay.EndTime.ToString(@"hh\:mm"),
                SlotDuration = firstDay.SlotDuration,
                IsActive = activeDays.Any()
            };
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
            // Don't allow past dates
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (date < today)
                return Enumerable.Empty<SlotDto>();

            // 1. Check if there's an exception for this date
            var exception = await _exceptionRepository.GetByDoctorIdAndDateAsync(doctorId, date);
            if (exception != null && !exception.IsAvailable)
                return Enumerable.Empty<SlotDto>(); // On leave

            // 2. Get weekly schedule for this day
            var dayOfWeek = (int)date.DayOfWeek;
            var availability = await _availabilityDayRepository.GetByDoctorIdAndDayAsync(doctorId, dayOfWeek);
            if (availability == null || !availability.IsActive)
                return Enumerable.Empty<SlotDto>();

            // 3. Validate slot duration
            if (availability.SlotDuration <= 0 || availability.SlotDuration > 480)
                return Enumerable.Empty<SlotDto>();

            // 4. Use exception hours if provided, otherwise use weekly schedule
            var startTime = exception?.CustomStartTime != null
                           ? TimeOnly.FromTimeSpan(exception.CustomStartTime.Value)
                           : TimeOnly.FromTimeSpan(availability.StartTime);

            var endTime = exception?.CustomEndTime != null
                            ? TimeOnly.FromTimeSpan(exception.CustomEndTime.Value)
                            : TimeOnly.FromTimeSpan(availability.EndTime);

            // 5. Validate time range
            if (endTime <= startTime)
                return Enumerable.Empty<SlotDto>();

            // 6. Fetch existing appointments for the doctor on that date
            var bookedSlots = await _appointmentRepository
                .GetAppointmentsForDoctorAndDateAsync(doctorId, date);

            var slots = new List<SlotDto>();
            var cursor = startTime;

            while (cursor.AddMinutes(availability.SlotDuration) <= endTime)
            {
                var slotStart = cursor;
                var slotEnd = cursor.AddMinutes(availability.SlotDuration);

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

        public async Task<AvailabilityResponseDto?> UpsertAvailabilityAsync(string keycloakId, UpdateAvailabilityRequest request)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("KeycloakId cannot be empty.", nameof(keycloakId));

            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId)
                ?? throw new InvalidOperationException("Doctor not found for the current user.");

            // Validate time formats
            if (!TimeOnly.TryParse(request.StartTime, out var startTime))
                throw new ArgumentException("Invalid StartTime format. Expected HH:mm format (e.g., 09:00).", nameof(request));

            if (!TimeOnly.TryParse(request.EndTime, out var endTime))
                throw new ArgumentException("Invalid EndTime format. Expected HH:mm format (e.g., 17:00).", nameof(request));

            // Validate time range
            if (endTime <= startTime)
                throw new ArgumentException("EndTime must be after StartTime.", nameof(request));

            // Validate slot duration
            if (request.SlotDuration <= 0)
                throw new ArgumentException("SlotDuration must be greater than 0.", nameof(request));

            if (request.SlotDuration > 480)
                throw new ArgumentException("SlotDuration cannot exceed 480 minutes (8 hours).", nameof(request));

            // Get existing availability days for this doctor
            var existingDays = await _availabilityDayRepository.GetByDoctorIdAsync(doctor.Id);
            var existingDaysDict = existingDays.ToDictionary(ad => ad.DayOfWeek);

            // Process each working day from request
            foreach (var dayName in request.WorkingDays ?? new List<string>())
            {
                var dayOfWeek = GetDayOfWeekFromName(dayName);
                if (dayOfWeek < 0) continue; // Skip invalid day names

                if (existingDaysDict.TryGetValue(dayOfWeek, out var existingDay))
                {
                    // Update existing day
                    existingDay.StartTime = startTime.ToTimeSpan();
                    existingDay.EndTime = endTime.ToTimeSpan();
                    existingDay.SlotDuration = request.SlotDuration;
                    existingDay.IsActive = request.AcceptBookings;
                    _availabilityDayRepository.UpdateAsync(existingDay);
                }
                else
                {
                    // Create new day
                    var newDay = new AvailabilityDay
                    {
                        DoctorId = doctor.Id,
                        DayOfWeek = dayOfWeek,
                        StartTime = startTime.ToTimeSpan(),
                        EndTime = endTime.ToTimeSpan(),
                        SlotDuration = request.SlotDuration,
                        IsActive = request.AcceptBookings
                    };
                    await _availabilityDayRepository.AddAsync(newDay);
                }
            }

            // Deactivate days that are no longer in the working days list
            var requestedDayOfWeeks = (request.WorkingDays ?? new List<string>())
                .Select(GetDayOfWeekFromName)
                .Where(d => d >= 0)
                .ToHashSet();

            foreach (var existingDay in existingDays)
            {
                if (!requestedDayOfWeeks.Contains(existingDay.DayOfWeek))
                {
                    existingDay.IsActive = false;
                    _availabilityDayRepository.UpdateAsync(existingDay);
                }
            }

            await _availabilityDayRepository.SaveChangesAsync();

            return await GetAvailabilityByDoctorIdAsync(doctor.Id);
        }

        
        public async Task<AvailabilityResponseDto?> UpsertAvailabilityDaysAsync(string keycloakId, List<AvailabilityDayRequest> days)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("KeycloakId cannot be empty.", nameof(keycloakId));

            if (days == null || !days.Any())
                throw new ArgumentException("At least one availability day is required.", nameof(days));

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId)
                ?? throw new InvalidOperationException("Doctor not found for the current user.");

            var existingDays = await _availabilityDayRepository.GetByDoctorIdAsync(doctor.Id);
            var existingDaysDict = existingDays.ToDictionary(ad => ad.DayOfWeek);

            foreach (var dayRequest in days)
            {
                // Validate day of week
                if (dayRequest.DayOfWeek < 0 || dayRequest.DayOfWeek > 6)
                    throw new ArgumentException($"Invalid DayOfWeek: {dayRequest.DayOfWeek}. Must be between 0 (Sunday) and 6 (Saturday).");

                // Validate and parse time formats
                TimeOnly startTime;
                TimeOnly endTime;

                if (!TimeOnly.TryParse(dayRequest.StartTime, out startTime))
                {
                    // Try parsing as 12-hour format with AM/PM
                    if (DateTime.TryParseExact(dayRequest.StartTime.Trim(),
                        new[] { "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt", "H:mm", "HH:mm" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var startDateTime))
                    {
                        startTime = TimeOnly.FromDateTime(startDateTime);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid StartTime format: '{dayRequest.StartTime}'. Expected HH:mm (e.g., 09:00) or h:mm AM/PM (e.g., 9:00 AM).");
                    }
                }

                if (!TimeOnly.TryParse(dayRequest.EndTime, out endTime))
                {
                    // Try parsing as 12-hour format with AM/PM
                    if (DateTime.TryParseExact(dayRequest.EndTime.Trim(),
                        new[] { "h:mm tt", "hh:mm tt", "h:mmtt", "hh:mmtt", "H:mm", "HH:mm" },
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var endDateTime))
                    {
                        endTime = TimeOnly.FromDateTime(endDateTime);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid EndTime format: '{dayRequest.EndTime}'. Expected HH:mm (e.g., 17:00) or h:mm AM/PM (e.g., 5:00 PM).");
                    }
                }

                // Validate time range
                if (endTime <= startTime)
                    throw new ArgumentException($"EndTime must be after StartTime for day {dayRequest.DayOfWeek}.");

                // Validate slot duration
                if (dayRequest.SlotDuration <= 0)
                    throw new ArgumentException("SlotDuration must be greater than 0.");

                if (dayRequest.SlotDuration > 480)
                    throw new ArgumentException("SlotDuration cannot exceed 480 minutes (8 hours).");

                // Update or create the day
                if (existingDaysDict.TryGetValue(dayRequest.DayOfWeek, out var existingDay))
                {
                    existingDay.StartTime = startTime.ToTimeSpan();
                    existingDay.EndTime = endTime.ToTimeSpan();
                    existingDay.SlotDuration = dayRequest.SlotDuration;
                    existingDay.IsActive = dayRequest.IsActive;
                    _availabilityDayRepository.UpdateAsync(existingDay);
                }
                else
                {
                    var newDay = new AvailabilityDay
                    {
                        DoctorId = doctor.Id,
                        DayOfWeek = dayRequest.DayOfWeek,
                        StartTime = startTime.ToTimeSpan(),
                        EndTime = endTime.ToTimeSpan(),
                        SlotDuration = dayRequest.SlotDuration,
                        IsActive = dayRequest.IsActive
                    };
                    await _availabilityDayRepository.AddAsync(newDay);
                }
            }

            await _availabilityDayRepository.SaveChangesAsync();

            return await GetAvailabilityByDoctorIdAsync(doctor.Id);
        }

        public async Task<IEnumerable<AvailabilityDayResponseDto>> GetAvailabilityDaysByKeycloakIdAsync(string keycloakId)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("KeycloakId cannot be empty.", nameof(keycloakId));

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId);
            if (doctor == null)
                return Enumerable.Empty<AvailabilityDayResponseDto>();

            var availabilityDays = await _availabilityDayRepository.GetByDoctorIdAsync(doctor.Id);
            
            return availabilityDays.Select(ad => new AvailabilityDayResponseDto
            {
                DayOfWeek = ad.DayOfWeek,
                StartTime = TimeOnly.FromTimeSpan(ad.StartTime).ToString("HH:mm"),
                EndTime = TimeOnly.FromTimeSpan(ad.EndTime).ToString("HH:mm"),
                SlotDuration = ad.SlotDuration,
                IsActive = ad.IsActive
            });
        }

        public async Task<bool> DeleteAvailabilityAsync(string keycloakId)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("KeycloakId cannot be empty.", nameof(keycloakId));

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId)
                ?? throw new InvalidOperationException("Doctor not found for the current user.");

            var availabilityDays = await _availabilityDayRepository.GetByDoctorIdAsync(doctor.Id);
            if (!availabilityDays.Any())
                return false;

            // Deactivate all days
            foreach (var day in availabilityDays)
            {
                day.IsActive = false;
                _availabilityDayRepository.UpdateAsync(day);
            }

            await _availabilityDayRepository.SaveChangesAsync();
            return true;
        }

        // Helper methods
        private int GetDayOfWeekFromName(string dayName)
        {
            if (string.IsNullOrWhiteSpace(dayName))
                return -1;

            var normalized = dayName.Trim().ToUpperInvariant();
            return normalized switch
            {
                "SUNDAY" => 0,
                "MONDAY" => 1,
                "TUESDAY" => 2,
                "WEDNESDAY" => 3,
                "THURSDAY" => 4,
                "FRIDAY" => 5,
                "SATURDAY" => 6,
                _ => -1
            };
        }

        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                0 => "Sunday",
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                _ => "Unknown"
            };
        }
    }
}
