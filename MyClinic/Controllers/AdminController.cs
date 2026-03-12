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
        private readonly ILeaveService _leaveService;

        public AdminController(IDoctorService doctorService, ISlotConfigService slotConfigService, ILeaveService leaveService)
        {
            _doctorService = doctorService;
            _slotConfigService = slotConfigService;
            _leaveService = leaveService;
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

        [HttpGet("leaves")]
        public async Task<IActionResult> GetAllLeaves()
        {

            var leaves = await _leaveService.GetAllLeavesAsync();
            return Ok(leaves);
        }
        [HttpGet("leaves/pending")]
        public async Task<IActionResult> GetPendingLeaves()
        {
            var leaves = await _leaveService.GetPendingLeavesAsync();
            return Ok(leaves);
        }

        [HttpPut("leaves/{leaveId:int}/approve")]
        public async Task<IActionResult> ApproveLeave(int leaveId)
        {

            var leave = await _leaveService.ApproveLeaveAsync(leaveId);
            if (leave == null)
                return NotFound(new { Message = "Leave not found" });

            return Ok(leave);
        }

        [HttpPut("leaves/{leaveId:int}/reject")]
        public async Task<IActionResult> RejectLeave(int leaveId)
        {

            var leave = await _leaveService.RejectLeaveAsync(leaveId);
            if (leave == null)
            {
                // Leave was deleted (rejected) - this is expected behavior
                return Ok(new { Message = "Leave rejected and removed successfully" });
            }

            return Ok(leave);
        }
    }
}

