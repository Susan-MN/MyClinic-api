using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyClinic.Application.DTO;
using MyClinic.Infrastructure.Interfaces.Services;
using MyClinic.Infrastructure.Servives;

namespace MyClinic.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    
    public class AppointmentsController : Controller
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }
       
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> BookAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                var patientKeycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                        ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(patientKeycloakId))
                    return Unauthorized(new { Message = "User not authenticated" });

                var result = await _appointmentService.BookAppointmentAsync(patientKeycloakId, request);
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
                return StatusCode(500, new { Message = "Failed to book appointment", Error = ex.Message });
            }
        }
    }
}