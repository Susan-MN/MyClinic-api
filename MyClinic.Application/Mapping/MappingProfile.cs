using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MyClinic.Application.DTO;
using MyClinic.Domain.Entities;

namespace MyClinic.Application.Mapping
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {
            
            CreateMap<SyncProfileRequest, Doctor>()
                .ForMember(dest => dest.KeycloakId, opt => opt.MapFrom(src => src.KeycloakId))
                .ForMember(
                    dest => dest.Specialty,
                    opt => opt.Ignore()
                )
                .ForMember(
                    dest => dest.Username,
                    opt => opt.MapFrom(src => src.Username) 
                )
                .ForMember(
                    dest => dest.Email,
                    opt => opt.MapFrom(src => src.Email)
                )
                .ForMember(
                    dest => dest.Status,
                    opt => opt.MapFrom(src => DoctorStatus.Pending)
                );

            CreateMap<SyncProfileRequest, User>()
                .ForMember(dest => dest.KeycloakId, opt => opt.MapFrom(src => src.KeycloakId));

            CreateMap<SyncProfileRequest, Admin>()
                .ForMember(dest => dest.KeycloakId, opt => opt.MapFrom(src => src.KeycloakId));

            CreateMap<Doctor, DoctorResponseDto>()
                 .ForMember(dest => dest.KeycloakId, opt => opt.MapFrom(src => src.KeycloakId))
                 .ForMember(dest => dest.Specialty, opt => opt.MapFrom(src => src.Specialty));
        }
    }
}
