using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Interfaces.Services;
using System.Security.Claims;
using System.Globalization;

namespace MyClinic.Controllers
{
    [ApiController]
    [Route("api/doctors")]
    public class DoctorsController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly IAvailabilityService _availabilityService;
        private readonly IAppointmentService _appointmentService;
        private readonly ISlotConfigService _slotConfigService;
        private readonly ILeaveService _leaveService;

        public DoctorsController(
            IDoctorService doctorService,
            IAvailabilityService availabilityService,
            IAppointmentService appointmentService,
            ISlotConfigService slotConfigService,
            ILeaveService leaveService)
        {
            _doctorService = doctorService;
            _availabilityService = availabilityService;
            _appointmentService = appointmentService;
            _slotConfigService = slotConfigService;
            _leaveService = leaveService;
        }




        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetApprovedDoctors()
        {
            var doctors = await _doctorService.GetApprovedDoctorsAsync();
            return Ok(doctors);
        }




        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            var doctor = await _doctorService.GetDoctorByKeycloakIdAsync(keycloakId);

            var response = doctor ?? new DoctorResponseDto
            {
                Id = 0,
                Username = string.Empty,
                Specialty = null,  
                Email = string.Empty,
                KeycloakId = keycloakId,
                Status = DoctorStatus.Pending,
                ProfileComplete = false
            };


            return Ok(response);
        }
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateDoctorRequest request)
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

           
            var updatedDoctor = await _doctorService.UpdateDoctorProfileAsync(keycloakId, request);

            if (updatedDoctor == null)
                return NotFound(new { Message = "Doctor profile not found" });

            return Ok(updatedDoctor);
        }

        [HttpGet("me/availability")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> GetMyAvailability()
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            // Check doctor status first
            var doctor = await _doctorService.GetDoctorByKeycloakIdAsync(keycloakId);
            if (doctor == null)
                return NotFound(new { Message = "Doctor profile not found" });
            // If doctor is not approved, return appropriate message
            if (doctor.Status != DoctorStatus.Approved)
            {
                return Ok(new
                {
                    Message = "Your profile is pending approval. Availability will be available after admin approval.",
                    Status = doctor.Status.ToString().ToLower(),
                    Availability = (AvailabilityResponseDto?)null
                });
            }

            var availability = await _availabilityService.GetAvailabilityByKeycloakIdAsync(keycloakId);
            if (availability == null)
                return NotFound(new { Message = "Availability not found" });

            return Ok(availability);
        }

        [HttpPost("me/availability")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> SaveMyAvailability([FromBody] UpdateAvailabilityRequest request)
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            if (request == null)
                return BadRequest(new { Message = "Request body cannot be null" });



            try
            {
                var result = await _availabilityService.UpsertAvailabilityAsync(keycloakId, request);
                if (result == null)
                    return BadRequest(new { Message = "Failed to save availability. Please check your input." });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while saving availability", Error = ex.Message });
            }
        }

        [HttpGet("me/availability/days")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> GetMyAvailabilityDays()
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            var doctor = await _doctorService.GetDoctorByKeycloakIdAsync(keycloakId);
            if (doctor == null)
                return NotFound(new { Message = "Doctor profile not found" });

            var availabilityDays = await _availabilityService.GetAvailabilityDaysByKeycloakIdAsync(keycloakId);

            return Ok(availabilityDays);
        }

        [HttpPost("me/availability/days")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> SaveMyAvailabilityDays([FromBody] AvailabilityDaysRequest request)
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            if (request?.Days == null || !request.Days.Any())
                return BadRequest(new { Message = "At least one availability day is required" });

            try
            {
                var result = await _availabilityService.UpsertAvailabilityDaysAsync(keycloakId, request.Days);
                if (result == null)
                    return BadRequest(new { Message = "Failed to save availability. Please check your input." });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while saving availability", Error = ex.Message });
            }
        }

        [HttpGet("slot-config")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> GetSlotConfig()
        {
            var slotConfig = await _slotConfigService.GetSlotConfigAsync();
            return Ok(slotConfig);
        }

        [HttpGet("{doctorId:int}/availability")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctorAvailability(int doctorId)
        {
            if (doctorId <= 0)
                return BadRequest(new { Message = "Invalid doctor ID" });

            try
            {
                var availability = await _availabilityService.GetAvailabilityByDoctorIdAsync(doctorId);
                if (availability == null)
                    return NotFound(new { Message = "Availability not found" });

                return Ok(availability);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while fetching availability", Error = ex.Message });
            }
        }

        [HttpGet("{doctorId:int}/slots")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, [FromQuery] string date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return BadRequest(new { Message = "Date parameter is required" });

            if (!DateOnly.TryParse(date, out var appointmentDate))
                return BadRequest(new { Message = "Invalid date format (use yyyy-MM-dd)" });

            if (doctorId <= 0)
                return BadRequest(new { Message = "Invalid doctor ID" });

            try
            {
                var slots = await _availabilityService.GetAvailableSlotsAsync(doctorId, appointmentDate);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while fetching available slots", Error = ex.Message });
            }
        }
        [HttpPost("me/availability/exceptions/range")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> CreateExceptionRange([FromBody] CreateLeaveRequest request)
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            if (request == null)
                return BadRequest(new { Message = "Request body cannot be null" });

            // Check if ModelState is valid (FluentValidation errors)
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(new { Message = "Validation failed", Errors = errors });
            }

            try
            {
                var result = await _leaveService.CreateLeaveAsync(keycloakId, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while creating exception range", Error = ex.Message });
            }
        }

        [HttpGet("{doctorId:int}/appointments")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctorAppointments(int doctorId, [FromQuery] string? date = null)
        {
            IEnumerable<AppointmentResponseDto> appointments; // Declare once outside the if block

            if (string.IsNullOrWhiteSpace(date))
            {
                // If no date provided, return all appointments for the doctor
                appointments = await _appointmentService.GetAppointmentsByDoctorIdAsync(doctorId);
            }
            else
            {
                // Parse date and return appointments for that specific date
                if (!DateOnly.TryParse(date, out var appointmentDate))
                    return BadRequest(new { Message = "Invalid date format (use yyyy-MM-dd)" });

                appointments = await _appointmentService.GetAppointmentsByDoctorIdAndDateAsync(doctorId, appointmentDate);
            }

            return Ok(appointments);
        }


        [HttpGet("me/appointments")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            var appointments = await _appointmentService.GetAppointmentsByKeycloakIdAsync(keycloakId);
            return Ok(appointments);
        }

        [HttpPost("me/leaves")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> CreateLeave([FromBody] CreateLeaveRequest request)
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            if (request == null)
                return BadRequest(new { Message = "Request body cannot be null" });

            try
            {
                var result = await _leaveService.CreateLeaveAsync(keycloakId, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while creating leave", Error = ex.Message });
            }
        }

        [HttpGet("me/leaves")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> GetMyLeaves()
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            try
            {
                var leaves = await _leaveService.GetLeavesByKeycloakIdAsync(keycloakId);
                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while fetching leaves", Error = ex.Message });
            }
        }

        [HttpGet("me/leaves/{leaveId:int}")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> GetLeave(int leaveId)
        {
            try
            {
                var leave = await _leaveService.GetLeaveByIdAsync(leaveId);
                if (leave == null)
                    return NotFound(new { Message = "Leave not found" });

                return Ok(leave);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while fetching leave", Error = ex.Message });
            }
        }

        [HttpPut("me/leaves/{leaveId:int}")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> UpdateLeave(int leaveId, [FromBody] UpdateLeaveRequest request)
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            if (request == null)
                return BadRequest(new { Message = "Request body cannot be null" });

            try
            {
                var result = await _leaveService.UpdateLeaveAsync(keycloakId, leaveId, request);
                if (result == null)
                    return NotFound(new { Message = "Leave not found" });

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating leave", Error = ex.Message });
            }
        }

        [HttpDelete("me/leaves/{leaveId:int}")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> DeleteLeave(int leaveId)
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            try
            {
                var result = await _leaveService.DeleteLeaveAsync(keycloakId, leaveId);
                if (!result)
                    return NotFound(new { Message = "Leave not found" });

                return Ok(new { Message = "Leave deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting leave", Error = ex.Message });
            }
        }

       
        [HttpDelete("me/availability")]
        [Authorize(Policy = "DoctorPolicy")]
        public async Task<IActionResult> DeleteMyAvailability()
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            var result = await _availabilityService.DeleteAvailabilityAsync(keycloakId);
            if (!result)
                return NotFound(new { Message = "Availability not found" });

            return Ok(new { Message = "Availability deleted successfully" });
        }

    }
}

