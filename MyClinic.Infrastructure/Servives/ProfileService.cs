using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyClinic.Domain.DTO;
using MyClinic.Application.Interfaces.Repositories;
using MyClinic.Application.Interfaces.Services;
using MyClinic.Domain.Entities;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace MyClinic.Infrastructure.Servives
{
    public class ProfileService : IProfileService
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Doctor> _doctorRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProfileService> _logger;


        public ProfileService(
            IGenericRepository<User> userRepository,
            IGenericRepository<Doctor> doctorRepository,
             IMapper mapper
        )
        {
            _userRepository = userRepository;
            _doctorRepository = doctorRepository;
            _mapper = mapper;
        }

        public async Task SyncProfile(SyncProfileRequest request)
        {
            _logger.LogInformation("SyncProfile started for role {Role} and KeycloakId {KeycloakId}",
                              request.Role, request.KeycloakId);
            if (request.Role.ToLower() == "doctor")
            {
                var existingUser = await _doctorRepository.GetByKeycloakIdAsync(request.KeycloakId);
                if (existingUser != null)
                    return;
                //var newDoctor = new Doctor { Username = request.Username, Email = request.Email };
                var newDoctor = _mapper.Map<Doctor>(request);
              //newDoctor.SetKeycloakId(request.KeycloakId);
                await _doctorRepository.AddAsync(newDoctor);
                await _doctorRepository.SaveChangesAsync();
            }
            else if (request.Role.ToLower() == "patient")
            {
                var existingUser = await _userRepository.GetByKeycloakIdAsync(request.KeycloakId);
                if (existingUser != null)
                    return;
                //var newUser = new User { Username = request.Username, Email = request.Email };
                var newUser = _mapper.Map<User>(request);
              //newUser.SetKeycloakId(request.KeycloakId);
                await _userRepository.AddAsync(newUser);
                await _userRepository.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException("Invalid role provided.");
            }
            _logger.LogInformation("SyncProfile finished successfully");
        }

      
    }
}
