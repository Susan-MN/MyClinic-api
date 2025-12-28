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

        public DoctorsController(
            IDoctorService doctorService,
            IAvailabilityService availabilityService,
            IAppointmentService appointmentService,
            ISlotConfigService slotConfigService)
        {
            _doctorService = doctorService;
            _availabilityService = availabilityService;
            _appointmentService = appointmentService;
            _slotConfigService = slotConfigService;
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

            var result = await _availabilityService.UpsertAvailabilityAsync(keycloakId, request);
            return Ok(result);
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
            var availability = await _availabilityService.GetAvailabilityByDoctorIdAsync(doctorId);
            if (availability == null)
                return NotFound(new { Message = "Availability not found" });

            return Ok(availability);
        }

        [HttpGet("{doctorId:int}/slots")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableSlots(int doctorId, [FromQuery] string date)
        {
            if (!DateOnly.TryParse(date, out var appointmentDate))
                return BadRequest(new { Message = "Invalid date format (use yyyy-MM-dd)" });

            var slots = await _availabilityService.GetAvailableSlotsAsync(doctorId, appointmentDate);
            return Ok(slots);
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
       
    }
}

