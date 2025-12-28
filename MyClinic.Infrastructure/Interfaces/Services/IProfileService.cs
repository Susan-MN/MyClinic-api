using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyClinic.Application.DTO;

namespace MyClinic.Infrastructure.Interfaces.Services
{
    public interface IProfileService
    {
        Task SyncProfile(SyncProfileRequest request);
        Task<ProfileResponseDto?> GetMyProfileAsync(string keycloakId);
    }
}
