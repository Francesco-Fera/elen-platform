using WorkflowEngine.Application.DTOs.User;
using WorkflowEngine.Core.Entities;

namespace WorkflowEngine.Application.Interfaces.Auth;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> UpdateProfileAsync(UpdateProfileRequest request);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<bool> DeactivateUserAsync(Guid userId);
    Task<List<User>> SearchUsersAsync(string searchTerm, int limit = 10);
}
