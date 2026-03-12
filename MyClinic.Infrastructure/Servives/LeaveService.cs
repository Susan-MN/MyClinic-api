using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;

namespace MyClinic.Infrastructure.Servives
{
    public class LeaveService : ILeaveService
    {
        private readonly IAvailabilityExceptionRepository _exceptionRepository;
        private readonly IGenericRepository<Doctor> _doctorRepository;
        private readonly IAppointmentRepository _appointmentRepository;

        public LeaveService(
            IAvailabilityExceptionRepository exceptionRepository,
            IGenericRepository<Doctor> doctorRepository,
            IAppointmentRepository appointmentRepository)
        {
            _exceptionRepository = exceptionRepository;
            _doctorRepository = doctorRepository;
            _appointmentRepository = appointmentRepository;
        }

        public async Task<LeaveResponseDto> CreateLeaveAsync(string keycloakId, CreateLeaveRequest request)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("KeycloakId cannot be empty.", nameof(keycloakId));

            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId)
                ?? throw new InvalidOperationException("Doctor not found for the current user.");

            if (!DateOnly.TryParse(request.StartDate, out var startDate))
                throw new ArgumentException("Invalid StartDate format. Expected yyyy-MM-dd format.", nameof(request));

            if (!DateOnly.TryParse(request.EndDate, out var endDate))
                throw new ArgumentException("Invalid EndDate format. Expected yyyy-MM-dd format.", nameof(request));

            if (endDate < startDate)
                throw new ArgumentException("EndDate must be after or equal to StartDate.", nameof(request));

            // Check for overlapping leaves (exceptions with IsAvailable = false)
            var existingExceptions = await _exceptionRepository.GetByDoctorIdAndDateRangeAsync(doctor.Id, startDate, endDate);
            if (existingExceptions.Any())
                throw new InvalidOperationException("You already have a leave for this date range.");

            // Create exception records for each day in the range
            var currentDate = startDate;
            var firstException = (AvailabilityException?)null;

            while (currentDate <= endDate)
            {
                var exception = new AvailabilityException
                {
                    DoctorId = doctor.Id,
                    ExceptionDate = currentDate,
                    IsAvailable = false, // On leave
                    CustomStartTime = null,
                    CustomEndTime = null,
                    Reason = request.Reason,
                    Type = ExceptionType.Leave
                };

                await _exceptionRepository.AddAsync(exception);
                
                if (firstException == null)
                    firstException = exception;

                currentDate = currentDate.AddDays(1);
            }

            await _exceptionRepository.SaveChangesAsync();

            // Return DTO representing the leave range
            return new LeaveResponseDto
            {
                Id = $"leave-{firstException!.Id}",
                DoctorId = doctor.Id,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Reason = request.Reason,
                IsApproved = true, // Exceptions are immediately "approved" (active)
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public async Task<IEnumerable<LeaveResponseDto>> GetLeavesByKeycloakIdAsync(string keycloakId)
        {
            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId);
            if (doctor == null)
                return Enumerable.Empty<LeaveResponseDto>();

            var exceptions = await _exceptionRepository.GetByDoctorIdAsync(doctor.Id);
            var leaveExceptions = exceptions.Where(e => e.Type == ExceptionType.Leave && !e.IsAvailable);
            
            return GroupExceptionsIntoLeaves(leaveExceptions);
        }

        public async Task<LeaveResponseDto?> GetLeaveByIdAsync(int leaveId)
        {
            var exception = await _exceptionRepository.GetByIdAsync(leaveId);
            if (exception == null || exception.Type != ExceptionType.Leave || exception.IsAvailable)
                return null;

            return new LeaveResponseDto
            {
                Id = $"leave-{exception.Id}",
                DoctorId = exception.DoctorId,
                StartDate = exception.ExceptionDate.ToString("yyyy-MM-dd"),
                EndDate = exception.ExceptionDate.ToString("yyyy-MM-dd"), // Single day leave
                Reason = exception.Reason,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public async Task<LeaveResponseDto?> UpdateLeaveAsync(string keycloakId, int leaveId, UpdateLeaveRequest request)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("KeycloakId cannot be empty.", nameof(keycloakId));

            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null.");

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId)
                ?? throw new InvalidOperationException("Doctor not found for the current user.");

            // Get all exceptions that might be part of this leave
            var allExceptions = await _exceptionRepository.GetByDoctorIdAsync(doctor.Id);
            var leaveExceptions = allExceptions
                .Where(e => e.Type == ExceptionType.Leave && !e.IsAvailable)
                .OrderBy(e => e.ExceptionDate)
                .ToList();

            // Find the leave range that contains this exception ID
            var targetException = leaveExceptions.FirstOrDefault(e => e.Id == leaveId);
            if (targetException == null)
                return null;

            // Find all consecutive exceptions with same reason (they form a leave range)
            var leaveRange = FindConsecutiveLeaveRange(leaveExceptions, targetException);

            if (!DateOnly.TryParse(request.StartDate, out var startDate))
                throw new ArgumentException("Invalid StartDate format. Expected yyyy-MM-dd format.", nameof(request));

            if (!DateOnly.TryParse(request.EndDate, out var endDate))
                throw new ArgumentException("Invalid EndDate format. Expected yyyy-MM-dd format.", nameof(request));

            if (endDate < startDate)
                throw new ArgumentException("EndDate must be after or equal to StartDate.", nameof(request));

            // Delete existing exceptions in the range
            foreach (var ex in leaveRange)
            {
                _exceptionRepository.DeleteAsync(ex);
            }

            // Create new exceptions for the updated range
            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                var exception = new AvailabilityException
                {
                    DoctorId = doctor.Id,
                    ExceptionDate = currentDate,
                    IsAvailable = false,
                    Reason = request.Reason,
                    Type = ExceptionType.Leave
                };
                await _exceptionRepository.AddAsync(exception);
                currentDate = currentDate.AddDays(1);
            }

            await _exceptionRepository.SaveChangesAsync();

            return new LeaveResponseDto
            {
                Id = $"leave-{leaveId}",
                DoctorId = doctor.Id,
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd"),
                Reason = request.Reason,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public async Task<bool> DeleteLeaveAsync(string keycloakId, int leaveId)
        {
            if (string.IsNullOrWhiteSpace(keycloakId))
                throw new ArgumentException("KeycloakId cannot be empty.", nameof(keycloakId));

            var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId)
                ?? throw new InvalidOperationException("Doctor not found for the current user.");

            var exception = await _exceptionRepository.GetByIdAsync(leaveId);
            if (exception == null || exception.DoctorId != doctor.Id || exception.Type != ExceptionType.Leave)
                return false;

            // Find and delete all consecutive exceptions in the leave range
            var allExceptions = await _exceptionRepository.GetByDoctorIdAsync(doctor.Id);
            var leaveExceptions = allExceptions
                .Where(e => e.Type == ExceptionType.Leave && !e.IsAvailable)
                .OrderBy(e => e.ExceptionDate)
                .ToList();

            var leaveRange = FindConsecutiveLeaveRange(leaveExceptions, exception);

            foreach (var ex in leaveRange)
            {
                _exceptionRepository.DeleteAsync(ex);
            }

            await _exceptionRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsDoctorOnLeaveAsync(int doctorId, DateOnly date)
        {
            var exception = await _exceptionRepository.GetByDoctorIdAndDateAsync(doctorId, date);
            return exception != null && exception.Type == ExceptionType.Leave && !exception.IsAvailable;
        }

        public async Task<IEnumerable<LeaveResponseDto>> GetApprovedLeavesByDoctorIdAsync(int doctorId)
        {
            var exceptions = await _exceptionRepository.GetApprovedLeavesByDoctorIdAsync(doctorId);
            return GroupExceptionsIntoLeaves(exceptions);
        }

        public async Task<IEnumerable<LeaveResponseDto>> GetAllLeavesAsync()
        {
            var exceptions = await _exceptionRepository.GetAllLeavesAsync();
            return GroupExceptionsIntoLeaves(exceptions);
        }

        public async Task<IEnumerable<LeaveResponseDto>> GetPendingLeavesAsync()
        {
            var exceptions = await _exceptionRepository.GetPendingLeavesAsync();
            return GroupExceptionsIntoLeaves(exceptions);
        }

        public async Task<LeaveResponseDto?> ApproveLeaveAsync(int leaveId)
        {
            var exception = await _exceptionRepository.GetByIdAsync(leaveId);
            if (exception == null || exception.Type != ExceptionType.Leave)
                return null;

            // Exceptions are already "approved" (active) when created
            // This method is kept for API compatibility
            // Check for overlapping appointments
            var allExceptions = await _exceptionRepository.GetByDoctorIdAsync(exception.DoctorId);
            var leaveRange = FindConsecutiveLeaveRange(
                allExceptions.Where(e => e.Type == ExceptionType.Leave && !e.IsAvailable).OrderBy(e => e.ExceptionDate).ToList(),
                exception);

            foreach (var ex in leaveRange)
            {
                var appointments = await _appointmentRepository
                    .GetAppointmentsForDoctorAndDateAsync(ex.DoctorId, ex.ExceptionDate);

                var hasNonCancelledAppointments = appointments.Any(a => a.Status != AppointmentStatus.Cancelled);
                if (hasNonCancelledAppointments)
                {
                    throw new InvalidOperationException(
                        $"Cannot approve leave. Doctor has existing appointments on {ex.ExceptionDate:yyyy-MM-dd}. " +
                        "Please cancel or reschedule appointments first.");
                }
            }

            // Leave is already active (IsAvailable = false means on leave)
            return new LeaveResponseDto
            {
                Id = $"leave-{exception.Id}",
                DoctorId = exception.DoctorId,
                StartDate = leaveRange.First().ExceptionDate.ToString("yyyy-MM-dd"),
                EndDate = leaveRange.Last().ExceptionDate.ToString("yyyy-MM-dd"),
                Reason = exception.Reason,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public async Task<LeaveResponseDto?> RejectLeaveAsync(int leaveId)
        {
            var exception = await _exceptionRepository.GetByIdAsync(leaveId);
            if (exception == null || exception.Type != ExceptionType.Leave)
                return null;

            // Delete the leave (all exceptions in the range)
            var allExceptions = await _exceptionRepository.GetByDoctorIdAsync(exception.DoctorId);
            var leaveRange = FindConsecutiveLeaveRange(
                allExceptions.Where(e => e.Type == ExceptionType.Leave && !e.IsAvailable).OrderBy(e => e.ExceptionDate).ToList(),
                exception);

            foreach (var ex in leaveRange)
            {
                _exceptionRepository.DeleteAsync(ex);
            }

            await _exceptionRepository.SaveChangesAsync();
            return null;
        }

        // Helper methods
        private List<AvailabilityException> FindConsecutiveLeaveRange(List<AvailabilityException> allExceptions, AvailabilityException target)
        {
            var result = new List<AvailabilityException> { target };
            var targetDate = target.ExceptionDate;
            var targetReason = target.Reason;

            // Find consecutive dates before
            var currentDate = targetDate.AddDays(-1);
            while (true)
            {
                var prev = allExceptions.FirstOrDefault(e => e.ExceptionDate == currentDate && e.Reason == targetReason);
                if (prev == null) break;
                result.Insert(0, prev);
                currentDate = currentDate.AddDays(-1);
            }

            // Find consecutive dates after
            currentDate = targetDate.AddDays(1);
            while (true)
            {
                var next = allExceptions.FirstOrDefault(e => e.ExceptionDate == currentDate && e.Reason == targetReason);
                if (next == null) break;
                result.Add(next);
                currentDate = currentDate.AddDays(1);
            }

            return result;
        }

        private IEnumerable<LeaveResponseDto> GroupExceptionsIntoLeaves(IEnumerable<AvailabilityException> exceptions)
        {
            var sorted = exceptions.OrderBy(e => e.ExceptionDate).ToList();
            var leaves = new List<LeaveResponseDto>();

            for (int i = 0; i < sorted.Count; i++)
            {
                var start = sorted[i];
                var end = start;
                var j = i + 1;

                // Find consecutive dates with same reason
                while (j < sorted.Count && 
                       sorted[j].ExceptionDate == end.ExceptionDate.AddDays(1) &&
                       sorted[j].Reason == start.Reason)
                {
                    end = sorted[j];
                    j++;
                }

                leaves.Add(new LeaveResponseDto
                {
                    Id = $"leave-{start.Id}",
                    DoctorId = start.DoctorId,
                    StartDate = start.ExceptionDate.ToString("yyyy-MM-dd"),
                    EndDate = end.ExceptionDate.ToString("yyyy-MM-dd"),
                    Reason = start.Reason,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                });

                i = j - 1; // Skip processed exceptions
            }

            return leaves;
        }
    }
}
