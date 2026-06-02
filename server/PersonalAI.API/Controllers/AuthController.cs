using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PersonalAI.Application.Common.Models;
using PersonalAI.Application.Common.Settings;
using PersonalAI.Application.Features.Auth.Commands.Login;
using PersonalAI.Application.Features.Auth.Commands.Logout;
using PersonalAI.Application.Features.Auth.Commands.Register;
using PersonalAI.Application.Features.Auth.Commands.RefreshToken;
using PersonalAI.Application.Features.Auth.Commands.UpdateProfile;
using PersonalAI.Application.Features.Auth.DTOs;
using PersonalAI.Application.Features.Auth.Queries.GetProfile;

namespace PersonalAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IMediator mediator, IOptions<JwtSettings> jwtSettings)
    {
        _mediator = mediator;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        SetAccessTokenCookie(result.AccessToken);
        SetRefreshTokenCookie(result.RefreshToken);

        var data = new { result.AccessToken, result.User };
        return Ok(ApiResponse<object>.SuccessResult(data, "User registered successfully", 200));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        SetAccessTokenCookie(result.AccessToken);
        SetRefreshTokenCookie(result.RefreshToken);

        var data = new { result.AccessToken, result.User };
        return Ok(ApiResponse<object>.SuccessResult(data, "User logged in successfully", 200));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = GetUserId();
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(ApiResponse<object>.FailureResult("Invalid request state.", 400));
        }

        await _mediator.Send(new LogoutCommand(userId, refreshToken));
        ClearAccessTokenCookie();
        ClearRefreshTokenCookie();

        return Ok(ApiResponse<string>.SuccessResult("Logged out successfully.", "Logged out successfully", 200));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(ApiResponse<object>.FailureResult("Refresh token cookie is required.", 400));
        }

        // Try to get Access Token from cookie first, fall back to Authorization header
        var accessToken = Request.Cookies["accessToken"];
        if (string.IsNullOrEmpty(accessToken))
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                accessToken = authHeader["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(ApiResponse<object>.FailureResult("Access token is required.", 400));
        }

        var result = await _mediator.Send(new RefreshTokenCommand(accessToken, refreshToken));
        SetAccessTokenCookie(result.AccessToken);
        SetRefreshTokenCookie(result.RefreshToken);

        var data = new { result.AccessToken, result.User };
        return Ok(ApiResponse<object>.SuccessResult(data, "Token refreshed successfully", 200));
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResult("Unauthorized access.", 401));
        }

        var profile = await _mediator.Send(new GetProfileQuery(userId));
        return Ok(ApiResponse<UserDto>.SuccessResult(profile, "Profile retrieved successfully", 200));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResult("Unauthorized access.", 401));
        }

        var command = new UpdateProfileCommand(userId, request.FirstName, request.LastName);
        var profile = await _mediator.Send(command);

        return Ok(ApiResponse<UserDto>.SuccessResult(profile, "Profile updated successfully", 200));
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private void SetAccessTokenCookie(string accessToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Requires HTTPS (active under local HTTPS redirection)
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
        };
        Response.Cookies.Append("accessToken", accessToken, cookieOptions);
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Requires HTTPS (active under local HTTPS redirection)
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private void ClearAccessTokenCookie()
    {
        Response.Cookies.Delete("accessToken");
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken");
    }
}
