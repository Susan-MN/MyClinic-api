using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyClinic.Domain.DTO;
using MyClinic.Application.Interfaces.Repositories;
using MyClinic.Application.Interfaces.Services;
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

    [HttpPost("sync")]
    public async Task<IActionResult> SyncProfile([FromBody] SyncProfileRequest request)
    {
        await _profileService.SyncProfile(request);
        return Ok(new { Message = "Profile Synced Successfully" });
    }
}
