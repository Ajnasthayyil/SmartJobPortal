using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IUserRepository
{
    Task SaveRefreshToken(int userId, string token, DateTime expiry);
    Task<RefreshToken?> GetRefreshToken(string token);
    Task RevokeRefreshToken(string token);
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByEmailAsync(string email);
    Task<int> GetRoleIdByName(string role);
    Task UpdatePhoneNumberAsync(int userId, string phoneNumber);
    Task UpdateProfileAsync(int userId, string fullName, string phoneNumber);
    Task UpdateProfilePictureAsync(int userId, string url);
    Task SetResetTokenAsync(string email, string token, DateTime expiry);
    Task<User?> GetByResetTokenAsync(string token);
    Task UpdatePasswordAsync(int userId, string newPasswordHash);
    Task CreateUserAsync(User user);
}