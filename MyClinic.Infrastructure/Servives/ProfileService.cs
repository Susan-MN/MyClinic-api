using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyClinic.Application.DTO;
using MyClinic.Infrastructure.Interfaces.Repositories;
using MyClinic.Infrastructure.Interfaces.Services;
using MyClinic.Domain.Entities;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace MyClinic.Infrastructure.Servives
{
    public class ProfileService : IProfileService
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Doctor> _doctorRepository;
        private readonly IGenericRepository<Admin> _adminRepository;
        private readonly IAvailabilityRepository _availabilityRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProfileService> _logger;


        public ProfileService(
            IGenericRepository<User> userRepository,
            IGenericRepository<Doctor> doctorRepository,
            IGenericRepository<Admin> adminRepository,
            IAvailabilityRepository availabilityRepository,
             IMapper mapper,
              ILogger<ProfileService> logger
        )
        {
            _userRepository = userRepository;
            _doctorRepository = doctorRepository;
            _adminRepository = adminRepository;
            _availabilityRepository = availabilityRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task SyncProfile(SyncProfileRequest request)
        {
            _logger.LogInformation("SyncProfile started for role {Role} and KeycloakId {KeycloakId}",
                              request.Role, request.KeycloakId);
            if (request.Role.ToLower() == "doctor")
            {
                try
                {
                    var existingUser = await _doctorRepository.GetByKeycloakIdAsync(request.KeycloakId);
                    if (existingUser != null)
                        return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching doctor by KeycloakId {KeycloakId}", request.KeycloakId);
                    throw;
                }
                //var newDoctor = new Doctor { Username = request.Username, Email = request.Email };
                var newDoctor = _mapper.Map<Doctor>(request);
                newDoctor.Specialty = null;
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
            else if (request.Role.ToLower() == "admin")
            {
                var existingAdmin = await _adminRepository.GetByKeycloakIdAsync(request.KeycloakId);
                if (existingAdmin != null)
                    return;
                
                var newAdmin = _mapper.Map<Admin>(request);
                await _adminRepository.AddAsync(newAdmin);
                await _adminRepository.SaveChangesAsync();
                _logger.LogInformation("Admin user synced to database");
            }
            else
            {
                throw new ArgumentException("Invalid role provided.");
            }
            _logger.LogInformation("SyncProfile finished successfully");
        }

        public async Task<ProfileResponseDto?> GetMyProfileAsync(string keycloakId)
        {
            try
            {
                if (string.IsNullOrEmpty(keycloakId))
                {
                    _logger.LogWarning("GetMyProfileAsync called with null or empty keycloakId");
                    return null;
                }

                _logger.LogInformation("GetMyProfileAsync called for KeycloakId: {KeycloakId}", keycloakId);
                
                var doctor = await _doctorRepository.GetByKeycloakIdAsync(keycloakId);
                if (doctor != null)
                {
                    _logger.LogInformation("Found doctor profile for KeycloakId: {KeycloakId}", keycloakId);
                    
                    // Check if doctor profile is complete
                    // Required: Username, Email, Specialty, PhoneNumber, Bio, ImageUrl, and Availability setup
                    var hasRequiredFields = !string.IsNullOrWhiteSpace(doctor.Username) &&
                                          !string.IsNullOrWhiteSpace(doctor.Email) &&
                                          !string.IsNullOrWhiteSpace(doctor.Specialty) &&
                                          !string.IsNullOrWhiteSpace(doctor.PhoneNumber) &&
                                          !string.IsNullOrWhiteSpace(doctor.Bio) &&
                                          !string.IsNullOrWhiteSpace(doctor.ImageUrl);
                    
                    // Check if availability is set up
                    var availability = await _availabilityRepository.GetByDoctorIdAsync(doctor.Id);
                    var hasAvailability = availability != null && availability.IsActive;
                    
                    var profileComplete = hasRequiredFields && hasAvailability;
                    
                    return new ProfileResponseDto
                    {
                        Role = "doctor",
                        Status = doctor.Status.ToString().ToLower(),
                        ProfileComplete = profileComplete
                    };
                }

                var patient = await _userRepository.GetByKeycloakIdAsync(keycloakId);
                if (patient != null)
                {
                    _logger.LogInformation("Found patient profile for KeycloakId: {KeycloakId}", keycloakId);
                    
                    // Patient profile is complete if they have username and email (from sync)
                    var profileComplete = !string.IsNullOrWhiteSpace(patient.Username) &&
                                        !string.IsNullOrWhiteSpace(patient.Email);
                    
                    return new ProfileResponseDto
                    {
                        Role = "patient",
                        Status = null,
                        ProfileComplete = profileComplete
                    };
                }

                var admin = await _adminRepository.GetByKeycloakIdAsync(keycloakId);
                if (admin != null)
                {
                    _logger.LogInformation("Found admin profile for KeycloakId: {KeycloakId}", keycloakId);
                    
                    // Admin profile is complete if they have username and email (from sync)
                    var profileComplete = !string.IsNullOrWhiteSpace(admin.Username) &&
                                        !string.IsNullOrWhiteSpace(admin.Email);
                    
                    return new ProfileResponseDto
                    {
                        Role = "admin",
                        Status = null,
                        ProfileComplete = profileComplete
                    };
                }

                _logger.LogWarning("No profile found for KeycloakId: {KeycloakId}", keycloakId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for KeycloakId: {KeycloakId}", keycloakId);
                throw new Exception($"Error retrieving profile: {ex.Message}", ex);
            }
        }
      
    }
}
