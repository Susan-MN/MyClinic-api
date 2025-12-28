using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyClinic.Application.DTO;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;
using MyClinic.Domain.Entities;

[ApiController]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }
    //Synchronize the authenticated user's profile and role with the database
    [HttpPost("sync")]
    public async Task<IActionResult> SyncProfile([FromBody] SyncProfileRequest request)
    {
        await _profileService.SyncProfile(request);
        return Ok(new { Message = "Profile Synced Successfully" });
    }

 
    /// Get user's profile from database
    /// Returns role information to determine dashboard redirect
    
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        try
        {
            var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(keycloakId))
                return Unauthorized(new { Message = "User not authenticated" });

            var profile = await _profileService.GetMyProfileAsync(keycloakId);
            if (profile == null)
                return NotFound(new { Message = "Profile not found" });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving profile", Error = ex.Message });
        }
    }
}
