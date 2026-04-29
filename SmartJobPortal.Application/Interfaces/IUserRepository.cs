using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IUserRepository
{
    Task SaveRefreshToken(int userId, string token, DateTime expiry);
    Task<RefreshToken?> GetRefreshToken(string token);
    Task RevokeRefreshToken(string token);
    Task UpdateProfilePictureAsync(int userId, string url);
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<int> GetRoleIdByName(string role);
    Task CreateUserAsync(User user);
}