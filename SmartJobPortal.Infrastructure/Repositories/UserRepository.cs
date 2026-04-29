using Dapper;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using SmartJobPortal.Infrastructure.Data;

namespace SmartJobPortal.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _factory;

    public UserRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"SELECT u.UserId, u.FullName, u.Email, u.PasswordHash, u.PhoneNumber, u.RoleId, r.RoleName, u.ProfilePictureUrl, u.IsApproved
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleId = r.RoleId
                    WHERE u.Email = @Email";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"SELECT u.UserId, u.FullName, u.Email, u.PasswordHash, u.PhoneNumber, u.RoleId, r.RoleName, u.ProfilePictureUrl, u.IsApproved
                    FROM Users u
                    INNER JOIN Roles r ON u.RoleId = r.RoleId
                    WHERE u.UserId = @UserId";

        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { UserId = id });
    }

    public async Task<int> GetRoleIdByName(string role)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"SELECT RoleId 
                    FROM Roles 
                    WHERE RoleName = @Role";   //  FIXED for your schema

        var roleId = await connection.ExecuteScalarAsync<int?>(sql, new { Role = role });

        if (roleId == null)
            throw new Exception($"Role '{role}' not found");

        return roleId.Value;
    }

    public async Task CreateUserAsync(User user)
    {
        using var connection = _factory.CreateConnection();

        var sql = @"INSERT INTO Users 
                    (FullName, Email, PasswordHash, PhoneNumber, RoleId, IsActive, IsApproved, CreatedAt, ProfilePictureUrl)
                    VALUES 
                    (@FullName, @Email, @PasswordHash, @PhoneNumber, @RoleId, @IsActive, @IsApproved, @CreatedAt, @ProfilePictureUrl)";

        await connection.ExecuteAsync(sql, user);
    }
    public async Task SaveRefreshToken(int userId, string token, DateTime expiry)
    {
        var sql = @"INSERT INTO RefreshTokens (UserId, Token, ExpiryDate)
                VALUES (@UserId, @Token, @ExpiryDate)";

        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            UserId = userId,
            Token = token,
            ExpiryDate = expiry
        });
    }
    public async Task<RefreshToken?> GetRefreshToken(string token)
    {
        var sql = "SELECT * FROM RefreshTokens WHERE Token = @Token";

        using var connection = _factory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<RefreshToken>(sql, new { Token = token });
    }
    public async Task RevokeRefreshToken(string token)
    {
        var sql = "UPDATE RefreshTokens SET IsRevoked = 1 WHERE Token = @Token";

        using var connection = _factory.CreateConnection();
        await connection.ExecuteAsync(sql, new { Token = token });
    }

    public async Task UpdateProfilePictureAsync(int userId, string url)
    {
        using var connection = _factory.CreateConnection();
        var sql = "UPDATE Users SET ProfilePictureUrl = @Url WHERE UserId = @UserId";
        await connection.ExecuteAsync(sql, new { Url = url, UserId = userId });
    }
}