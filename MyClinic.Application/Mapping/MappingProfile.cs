using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MyClinic.Domain.DTO;
using MyClinic.Domain.Entities;

namespace MyClinic.Application.Mapping
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {
            
            CreateMap<SyncProfileRequest, Doctor>()
                .ForMember(dest => dest.KeycloakId, opt => opt.MapFrom(src => src.KeycloakId));

           
            CreateMap<SyncProfileRequest, User>()
                .ForMember(dest => dest.KeycloakId, opt => opt.MapFrom(src => src.KeycloakId));
        }
    }
}
