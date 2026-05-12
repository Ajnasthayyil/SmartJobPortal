using SmartJobPortal.Domain.Entities;

namespace SmartJobPortal.Application.Interfaces;

public interface IJwtService
{
    string GenerateJwtToken(User user);
    string GenerateRefreshToken();
}
