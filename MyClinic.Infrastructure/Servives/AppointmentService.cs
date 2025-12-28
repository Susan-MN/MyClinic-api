using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;
using System.Globalization;
using System.Linq;

namespace MyClinic.Infrastructure.Servives
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IGenericRepository<Doctor> _doctorRepository;
        private readonly IGenericRepository<User> _userRepository;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IGenericRepository<Doctor> doctorRepository,
            IGenericRepository<User> userRepository)
        {
            _appointmentRepository = appointmentRepository;
            _doctorRepository = doctorRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<AppointmentResponseDto>> GetAppointmentsByDoctorIdAsync(int doctorId)
        {
            var appointments = await _appointmentRepository.GetByDoctorIdAsync(doctorId);
            return appointments.Select(MapToDto);
        }

        public async Task<IEnumerable<AppointmentResponseDto>> GetAppointmentsByKeycloakIdAsync(string keycloakId)
        {
            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId);
            if (doctor == null)
                return Enumerable.Empty<AppointmentResponseDto>();

            // doctor must be approved AND profile must be complete
            if (doctor.Status != DoctorStatus.Approved)
            {
                throw new InvalidOperationException(
                    "Cannot view appointments. Your profile is pending admin approval.");
            }

            if (!doctor.ProfileComplete)
            {
                throw new InvalidOperationException(
                    "Cannot view appointments. Please complete your profile first.");
            }

            return await GetAppointmentsByDoctorIdAsync(doctor.Id);
        }

        public async Task<AppointmentResponseDto> BookAppointmentAsync(string patientKeycloakId, CreateAppointmentRequest request)
        {
            if (request == null)
                throw new ArgumentException("Request body is required.");

            // Validate patient exists
            var user = await _userRepository.GetByKeycloakIdAsync(patientKeycloakId)
                ?? throw new InvalidOperationException("Patient not found. Please complete your profile registration.");

            // Validate doctor exists and is approved
            var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId)
                         ?? throw new InvalidOperationException("Doctor not found.");

            if (doctor.Status != DoctorStatus.Approved)
                throw new InvalidOperationException("Cannot book appointment. Doctor is not approved.");

            // Validate date format
            if (string.IsNullOrWhiteSpace(request.AppointmentDate))
            {
                throw new ArgumentException("AppointmentDate is required and cannot be empty.");
            }

            // Parse appointment date
            DateTime dateTime;
            bool dateParsed = false;

            if (DateOnly.TryParse(request.AppointmentDate, out var dateOnly))
            {
                dateTime = DateTime.SpecifyKind(dateOnly.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                dateParsed = true;
            }
            // Try common date formats
            else if (DateTime.TryParseExact(request.AppointmentDate, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTime))
            {
                dateParsed = true;
            }
            else if (DateTime.TryParseExact(request.AppointmentDate, "M/d/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTime))
            {
                dateParsed = true;
            }
            else if (DateTime.TryParseExact(request.AppointmentDate, "MM/dd/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTime))
            {
                dateParsed = true;
            }
            // Fallback: try general parsing
            else if (DateTime.TryParse(request.AppointmentDate,
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateTime))
            {
                dateParsed = true;
            }

            if (!dateParsed)
            {
                throw new ArgumentException($"Invalid appointment date format: '{request.AppointmentDate}'. Supported formats: yyyy-MM-dd, M/d/yyyy, or MM/dd/yyyy");
            }

            var appointmentDateUtc = DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Utc);
            var appointmentDateOnly = DateOnly.FromDateTime(appointmentDateUtc);

            // Parse time strings to TimeSpan
            if (string.IsNullOrWhiteSpace(request.StartTime))
                throw new ArgumentException("StartTime is required and cannot be empty.");

            if (string.IsNullOrWhiteSpace(request.EndTime))
                throw new ArgumentException("EndTime is required and cannot be empty.");

            if (!TimeSpan.TryParse(request.StartTime, out var startTime))
                throw new ArgumentException($"Invalid StartTime format: '{request.StartTime}'. Expected format: HH:mm or HH:mm:ss");

            if (!TimeSpan.TryParse(request.EndTime, out var endTime))
                throw new ArgumentException($"Invalid EndTime format: '{request.EndTime}'. Expected format: HH:mm or HH:mm:ss");

            // Validate time range
            if (startTime >= endTime)
                throw new ArgumentException("StartTime must be before EndTime.");

            // Check for slot conflicts (prevent double-booking)
            var existingAppointments = await _appointmentRepository
                .GetAppointmentsForDoctorAndDateAsync(request.DoctorId, appointmentDateOnly);

            var hasConflict = existingAppointments.Any(a =>
            {
                var existingStart = a.StartTime;
                var existingEnd = a.EndTime;

                // Two time ranges overlap if: !(end1 <= start2 || start1 >= end2)
                return !(endTime <= existingStart || startTime >= existingEnd) &&
                       a.Status != AppointmentStatus.Cancelled;
            });

            if (hasConflict)
                throw new InvalidOperationException("This time slot is already booked. Please select another time.");

            // Create appointment
            var appointment = new Appointment
            {
                UserId = user.Id,
                PatientName = user.Username,
                DoctorId = request.DoctorId,
                SlotId = request.SlotId,
                AppointmentDate = appointmentDateUtc,
                StartTime = startTime,
                EndTime = endTime,
                Status = AppointmentStatus.Pending
            };

            await _appointmentRepository.AddAsync(appointment);
            await _appointmentRepository.SaveChangesAsync();

            return MapToDto(appointment);
        }

        public async Task<IEnumerable<AppointmentResponseDto>> GetAppointmentsByDoctorIdAndDateAsync(int doctorId, DateOnly date)
        {
            var appointments = await _appointmentRepository.GetAppointmentsForDoctorAndDateAsync(doctorId, date);
            return appointments.Select(MapToDto);
        }

        private AppointmentResponseDto MapToDto(Appointment appointment)
        {

            // Convert TimeSpan to "HH:mm" strings
            var startTimeString = appointment.StartTime.ToString(@"hh\:mm");
            var endTimeString = appointment.EndTime.ToString(@"hh\:mm");

            // Use UserId as string identifier for now
            var patientIdString = appointment.UserId.ToString();

            return new AppointmentResponseDto
            {
                Id = $"appt-{appointment.Id}",
                PatientId = patientIdString,
                PatientName = appointment.PatientName,
                DoctorId = appointment.DoctorId,
                SlotId = appointment.SlotId,
                AppointmentDate = appointment.AppointmentDate.ToString("yyyy-MM-dd"),
                StartTime = startTimeString,
                EndTime = endTimeString,
                Status = appointment.Status.ToString()
            };
        }
    }
}

