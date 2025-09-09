using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyClinic.Domain.DTO;

namespace MyClinic.Application.Interfaces.Services
{
    public interface IProfileService
    {
        Task SyncProfile(SyncProfileRequest request);
    }
}
