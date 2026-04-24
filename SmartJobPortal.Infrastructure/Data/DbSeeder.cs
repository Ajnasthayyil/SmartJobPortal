using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SmartJobPortal.Infrastructure.Services;

namespace SmartJobPortal.Infrastructure.Data;

public class DataSeeder
{
    private readonly IDbConnectionFactory _factory;
    private readonly IConfiguration _config;
    private readonly PasswordHasher _hasher;

    public DataSeeder(IDbConnectionFactory factory, IConfiguration config)
    {
        _factory = factory;
        _config = config;
        _hasher = new PasswordHasher();
    }

    // ── Entry point — called from Program.cs on every startup ────────────────
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminAsync();
    }

    // ── 1. Seed Roles ────────────────────────────────────────────────────────
    // Ensures Admin, Recruiter, Candidate roles always exist
    private async Task SeedRolesAsync()
    {
        using var conn = _factory.CreateConnection();

        var roles = new[] { "Admin", "Recruiter", "Candidate" };

        foreach (var role in roles)
        {
            var exists = await conn.ExecuteScalarAsync<bool>("""
                SELECT CAST(COUNT(1) AS BIT)
                FROM Roles
                WHERE RoleName = @RoleName
                """, new { RoleName = role });

            if (!exists)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO Roles (RoleName)
                    VALUES (@RoleName)
                    """, new { RoleName = role });

                Console.WriteLine($"[Seeder] Role '{role}' created.");
            }
        }
    }

    // ── 2. Seed Admin user ───────────────────────────────────────────────────
    // Only creates Admin if no Admin account exists — safe to run on every startup
    private async Task SeedAdminAsync()
    {
        using var conn = _factory.CreateConnection();

        // Check if admin already exists
        var adminExists = await conn.ExecuteScalarAsync<bool>("""
            SELECT CAST(COUNT(1) AS BIT)
            FROM Users u
            INNER JOIN Roles r ON r.RoleId = u.RoleId
            WHERE r.RoleName = 'Admin'
            """);

        if (adminExists)
        {
            Console.WriteLine("[Seeder] Admin already exists — skipping.");
            return;
        }

        // Read credentials from appsettings
        var fullName = _config["AdminSeed:FullName"]
            ?? throw new InvalidOperationException(
                "AdminSeed:FullName is missing from appsettings.json");

        var email = _config["AdminSeed:Email"]
            ?? throw new InvalidOperationException(
                "AdminSeed:Email is missing from appsettings.json");

        var password = _config["AdminSeed:Password"]
            ?? throw new InvalidOperationException(
                "AdminSeed:Password is missing from appsettings.json");

        var phoneNumber = _config["AdminSeed:PhoneNumber"] ?? "0000000000";

        // Get Admin RoleId
        var adminRoleId = await conn.ExecuteScalarAsync<int>("""
            SELECT RoleId FROM Roles WHERE RoleName = 'Admin'
            """);

        if (adminRoleId == 0)
            throw new InvalidOperationException(
                "Admin role not found. SeedRolesAsync must run before SeedAdminAsync.");

        // Hash the password using your existing PasswordHasher
        var passwordHash = _hasher.Hash(password);

        // Insert Admin user
        await conn.ExecuteAsync("""
            INSERT INTO Users
                (FullName, Email, PasswordHash, PhoneNumber,
                 RoleId, IsActive, IsApproved, CreatedAt)
            VALUES
                (@FullName, @Email, @PasswordHash, @PhoneNumber,
                 @RoleId, 1, 1, GETDATE())
            """, new
        {
            FullName = fullName,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            PhoneNumber = phoneNumber,
            RoleId = adminRoleId
        });

        Console.WriteLine($"[Seeder] Admin account created.");
        Console.WriteLine($"[Seeder] Email    : {email}");
        Console.WriteLine($"[Seeder] Password : {password}");
        Console.WriteLine($"[Seeder] Change this password after first login!");
    }
}