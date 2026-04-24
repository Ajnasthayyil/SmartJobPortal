using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Interfaces;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    //  COOKIE METHOD (ONLY ONE)
    private void SetTokenCookie(string token, string refreshToken)
    {
        Response.Cookies.Append("AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(30)
        });

        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        });
    }

    //  REGISTER
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _service.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    //  LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _service.LoginAsync(request);

        if (!result.Success || result.Data == null)
            return Unauthorized(result);

        SetTokenCookie(result.Data.Token!, result.Data.RefreshToken!);

        return Ok(result);
    }

    //  REFRESH (FIXED)
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            Response.Cookies.Delete("AuthToken");
            Response.Cookies.Delete("RefreshToken");
            return Unauthorized(new { success = false, message = "Login required." });
        }

        var result = await _service.RefreshTokenAsync(refreshToken);

        if (!result.Success || result.Data == null)
        {
            Response.Cookies.Delete("AuthToken");
            Response.Cookies.Delete("RefreshToken");
            return Unauthorized(result);
        }

        SetTokenCookie(result.Data.Token!, result.Data.RefreshToken!);

        return Ok(result);
    }

    //  LOGOUT
    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");
        Response.Cookies.Delete("RefreshToken");

        return Ok(new { message = "Logged out successfully" });
    }
}