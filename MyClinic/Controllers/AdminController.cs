using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Interfaces.Services;

namespace MyClinic.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class AdminController : ControllerBase
    {
        private readonly IDoctorService _doctorService;
        private readonly ISlotConfigService _slotConfigService;

        public AdminController(IDoctorService doctorService, ISlotConfigService slotConfigService)
        {
            _doctorService = doctorService;
            _slotConfigService = slotConfigService;
        }

       
       
        
        [HttpGet("doctors")]
        public async Task<IActionResult> GetAllDoctors()
        {
            var doctors = await _doctorService.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        
        
      
        [HttpGet("doctors/{id}")]
        public async Task<IActionResult> GetDoctorById(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
                return NotFound(new { Message = "Doctor not found" });

            return Ok(doctor);
        }

       
        
        
        [HttpPut("doctors/{id}/approve")]
        public async Task<IActionResult> ApproveDoctor(int id)
        {
            try
            {
                var doctor = await _doctorService.UpdateDoctorStatusAsync(id, DoctorStatus.Approved);
                if (doctor == null)
                    return NotFound(new { Message = "Doctor not found" });

                return Ok(doctor);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        
       
        
        [HttpPut("doctors/{id}/reject")]
        public async Task<IActionResult> RejectDoctor(int id)
        {

            try
            {
                var doctor = await _doctorService.UpdateDoctorStatusAsync(id, DoctorStatus.Declined);
                if (doctor == null)
                    return NotFound(new { Message = "Doctor not found" });

                return Ok(doctor);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("slot-config")]
        public async Task<IActionResult> GetSlotConfig()
        {
            var slotConfig = await _slotConfigService.GetSlotConfigAsync();
            return Ok(slotConfig);
        }
    }
}

