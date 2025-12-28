using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;
using AutoMapper;
using System.Text.Json;

namespace MyClinic.Infrastructure.Servives
{
    public class SlotConfigService : ISlotConfigService
    {
        private readonly ISlotConfigRepository _slotConfigRepository;
        private readonly IMapper _mapper;

        public SlotConfigService(
            ISlotConfigRepository slotConfigRepository,
            IMapper mapper)
        {
            _slotConfigRepository = slotConfigRepository;
            _mapper = mapper;
        }

        public async Task<SlotConfigResponseDto?> GetSlotConfigAsync()
        {
            var slotConfig = await _slotConfigRepository.GetSingletonAsync();
            
            if (slotConfig == null)
            {
                // Return default configuration if none exists
                return new SlotConfigResponseDto
                {
                    Id = "slot-config-id",
                    AllowedDurations = new List<int> { 15, 30, 45, 60 },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            var dto = new SlotConfigResponseDto
            {
                Id = $"slot-config-{slotConfig.Id}",
                CreatedAt = slotConfig.CreatedAt,
                UpdatedAt = slotConfig.UpdatedAt
            };

            // Parse JSON array to List<int>
            try
            {
                dto.AllowedDurations = JsonSerializer.Deserialize<List<int>>(slotConfig.AllowedDurationsJson) ?? new List<int>();
            }
            catch
            {
                dto.AllowedDurations = new List<int> { 15, 30, 45, 60 };
            }

            return dto;
        }
    }
}

