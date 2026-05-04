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
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Set to true in production with HTTPS
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("AuthToken", token, cookieOptions);
        Response.Cookies.Append("RefreshToken", refreshToken, cookieOptions);
    }

    //  REGISTER
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _service.RegisterAsync(request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    //  LOGIN
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _service.LoginAsync(request);

        if (!result.Success || result.Data == null)
            return Unauthorized(result);

        SetTokenCookie(result.Data.Token!, result.Data.RefreshToken!);

        return Ok(result);
    }

    //GOOGLE LOGIN
    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var result = await _service.GoogleLoginAsync(request);

        if (!result.Success || result.Data == null)
            return Unauthorized(result);

        SetTokenCookie(result.Data.Token!, result.Data.RefreshToken!);

        return Ok(result);
    }

    //  REFRESH (FIXED)
    [AllowAnonymous]
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
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["RefreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _service.RevokeTokenAsync(refreshToken);
        }

        Response.Cookies.Delete("AuthToken");
        Response.Cookies.Delete("RefreshToken");

        return Ok(new { success = true, message = "Logged out successfully" });
    }
}