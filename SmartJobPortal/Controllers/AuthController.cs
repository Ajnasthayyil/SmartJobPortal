using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartJobPortal.Application.DTOs.Auth;
using SmartJobPortal.Application.Features.Auth.Commands.GoogleLogin;
using SmartJobPortal.Application.Features.Auth.Commands.Login;
using SmartJobPortal.Application.Features.Auth.Commands.RefreshToken;
using SmartJobPortal.Application.Features.Auth.Commands.Register;

namespace SmartJobPortal.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    //  COOKIE METHOD (ONLY ONE)
    private void SetTokenCookie(string token, string refreshToken)
    {
        // Access Token Cookie (Short-lived)
        var authOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false, 
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddMinutes(15) 
        };

        // Refresh Token Cookie (Long-lived)
        var refreshOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("AuthToken", token, authOptions);
        Response.Cookies.Append("RefreshToken", refreshToken, refreshOptions);
    }

    //  REGISTER
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(request));

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    //  LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request));

        if (!result.Success || result.Data == null)
            return Unauthorized(result);

        SetTokenCookie(result.Data.Token!, result.Data.RefreshToken!);

        return Ok(result);
    }

    //GOOGLE LOGIN
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var result = await _mediator.Send(new GoogleLoginCommand(request));

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

        var result = await _mediator.Send(new RefreshTokenCommand(refreshToken));

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