using MyClinic.Application.DTO;

namespace MyClinic.Infrastructure.Interfaces.Services
{
    public interface ISlotConfigService
    {
        Task<SlotConfigResponseDto?> GetSlotConfigAsync();
    }
}

