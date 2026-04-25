using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartJobPortal.Application.Common;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;
using SmartJobPortal.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;


namespace SmartJobPortal.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
    }

    //  STRONG REFRESH TOKEN
    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    //  REGISTER
    public async Task<ApiResponse<string>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _repo.GetByEmailAsync(request.Email!);

        if (existingUser != null)
        {
            return ApiResponse<string>.FailureResponse(
                new List<string> { "Email is already registered" },
                "Duplicate Email"
            );
        }

        var roleId = await _repo.GetRoleIdByName(request.Role!);

        var user = new User
        {
            FullName = request.FullName!,
            Email = request.Email!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!),
            PhoneNumber = request.PhoneNumber!,
            RoleId = roleId,
            IsActive = true,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.CreateUserAsync(user);

        return ApiResponse<string>.SuccessResponse(null, "User registered successfully");
    }

    //  LOGIN
    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _repo.GetByEmailAsync(request.Email!);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                new List<string> { "Invalid email or password" },
                "Authentication Failed"
            );
        }

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        await _repo.SaveRefreshToken(
            user.UserId,
            refreshToken,
            DateTime.UtcNow.AddDays(
                Convert.ToDouble(_config["Jwt:RefreshTokenExpiryDays"])
            )
        );

        return ApiResponse<AuthResponse>.SuccessResponse(
            new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            },
            "Login successful"
        );
    }

    //  REFRESH TOKEN (FIXED)
    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _repo.GetRefreshToken(refreshToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                new List<string> { "Session expired. Login required." }
            );
        }

        //  FIX: Fetch user
        var user = await _repo.GetByIdAsync(storedToken.UserId);

        if (user == null)
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                new List<string> { "User not found. Login required." }
            );
        }

        var newAccessToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        //  revoke old token
        await _repo.RevokeRefreshToken(refreshToken);

        //  save new token WITH expiry
        await _repo.SaveRefreshToken(
            user.UserId,
            newRefreshToken,
            DateTime.UtcNow.AddDays(
                Convert.ToDouble(_config["Jwt:RefreshTokenExpiryDays"])
            )
        );

        return ApiResponse<AuthResponse>.SuccessResponse(
            new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            },
            "Token refreshed"
        );
    }

    //  JWT GENERATION
    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.RoleName ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_config["Jwt:AccessTokenExpiryMinutes"])
            ),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public async Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
        }
        catch
        {
            return ApiResponse<AuthResponse>.FailureResponse(
                new List<string> { "Invalid Google token" }
            );
        }

        var email = payload.Email;
        var name = payload.Name;

        var user = await _repo.GetByEmailAsync(email);

        if (user == null)
        {
            var roleId = await _repo.GetRoleIdByName("Candidate");

            user = new User
            {
                FullName = name,
                Email = email,
                PasswordHash = "", // not required
                RoleId = roleId
            };

            await _repo.CreateUserAsync(user);
        }

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        await _repo.SaveRefreshToken(
            user.UserId,
            refreshToken,
            DateTime.UtcNow.AddDays(
                Convert.ToDouble(_config["Jwt:RefreshTokenExpiryDays"])
            )
        );

        return ApiResponse<AuthResponse>.SuccessResponse(
            new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            },
            "Google login successful"
        );
    }
    }