using MyClinic.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClinic.Infrastructure.Interfaces.Services
{
    public interface ILeaveService
    {
        Task<LeaveResponseDto> CreateLeaveAsync(string keycloakId, CreateLeaveRequest request);
        Task<IEnumerable<LeaveResponseDto>> GetLeavesByKeycloakIdAsync(string keycloakId);
        Task<LeaveResponseDto?> GetLeaveByIdAsync(int leaveId);
        Task<LeaveResponseDto?> UpdateLeaveAsync(string keycloakId, int leaveId, UpdateLeaveRequest request);
        Task<bool> DeleteLeaveAsync(string keycloakId, int leaveId);
        Task<bool> IsDoctorOnLeaveAsync(int doctorId, DateOnly date);
        Task<IEnumerable<LeaveResponseDto>> GetApprovedLeavesByDoctorIdAsync(int doctorId);
        Task<IEnumerable<LeaveResponseDto>> GetAllLeavesAsync();
        Task<IEnumerable<LeaveResponseDto>> GetPendingLeavesAsync();
        Task<LeaveResponseDto?> ApproveLeaveAsync(int leaveId);
        Task<LeaveResponseDto?> RejectLeaveAsync(int leaveId);
    }
}
