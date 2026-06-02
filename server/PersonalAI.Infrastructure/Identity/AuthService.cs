using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PersonalAI.Application.Common.Interfaces;
using PersonalAI.Application.Common.Settings;
using PersonalAI.Application.Features.Auth.DTOs;
using PersonalAI.Infrastructure.Persistence.Contexts;

namespace PersonalAI.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IPasswordHasher<ApplicationUser> passwordHasher,
        IJwtTokenService jwtTokenService,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var formattedEmail = request.Email.Trim().ToLower();
        _logger.LogInformation("Attempting to register user with email: {Email}", formattedEmail);

        var existingUser = await _context.Users.AnyAsync(u => u.Email == formattedEmail);
        if (existingUser)
        {
            _logger.LogWarning("Registration failed. Email {Email} is already registered.", formattedEmail);
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            Email = formattedEmail,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully registered user {UserId} with email {Email}.", user.Id, formattedEmail);

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var formattedEmail = request.Email.Trim().ToLower();
        _logger.LogInformation("Attempting login for email: {Email}", formattedEmail);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == formattedEmail);
        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", formattedEmail);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        _logger.LogInformation("Successful login for user {UserId} with email {Email}.", user.Id, formattedEmail);

        return await GenerateAuthResponseAsync(user);
    }

    public async Task LogoutAsync(string userId, string refreshTokenValue)
    {
        var intUserId = int.Parse(userId);
        _logger.LogInformation("User {UserId} is logging out.", intUserId);

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == intUserId && t.Token == refreshTokenValue && t.Revoked == null);

        if (token != null)
        {
            token.Revoked = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully revoked active refresh token for user {UserId}.", intUserId);
        }
        else
        {
            _logger.LogWarning("No active refresh token found for logout request for user {UserId}.", intUserId);
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string accessToken, string refreshTokenValue)
    {
        _logger.LogInformation("Attempting token refresh operation.");

        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
        {
            _logger.LogWarning("Token refresh failed: invalid or malformed access token.");
            throw new UnauthorizedAccessException("Invalid access token.");
        }

        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? principal.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            _logger.LogWarning("Token refresh failed: missing user ID claim in access token.");
            throw new UnauthorizedAccessException("Invalid token claims.");
        }

        var userId = int.Parse(userIdClaim.Value);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Token refresh failed: user {UserId} not found in database.", userId);
            throw new UnauthorizedAccessException("User not found.");
        }

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == refreshTokenValue);

        if (storedToken == null || !storedToken.IsActive)
        {
            _logger.LogWarning("Token refresh failed: refresh token for user {UserId} is invalid or expired.", userId);
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Token rotation: Revoke old token and generate new pair
        storedToken.Revoked = DateTime.UtcNow;
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        storedToken.ReplacedByToken = newRefreshTokenValue;

        var newAccessToken = _jwtTokenService.GenerateAccessToken(
            user.Id.ToString(),
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Role.ToString());

        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenValue,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            Created = DateTime.UtcNow,
            UserId = user.Id
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully completed token rotation for user {UserId}.", userId);

        return new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenValue,
            User = MapToUserDto(user)
        };
    }

    public async Task<UserDto> GetProfileAsync(string userId)
    {
        var intUserId = int.Parse(userId);
        _logger.LogInformation("Retrieving profile details for user {UserId}.", intUserId);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == intUserId);
        if (user == null)
        {
            _logger.LogWarning("Profile retrieval failed: user {UserId} not found.", intUserId);
            throw new KeyNotFoundException("User profile not found.");
        }

        return MapToUserDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequestDto request)
    {
        var intUserId = int.Parse(userId);
        _logger.LogInformation("Updating profile details for user {UserId}.", intUserId);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == intUserId);
        if (user == null)
        {
            _logger.LogWarning("Profile update failed: user {UserId} not found.", intUserId);
            throw new KeyNotFoundException("User profile not found.");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated profile for user {UserId}.", intUserId);

        return MapToUserDto(user);
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id.ToString(),
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Role.ToString());

        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            Created = DateTime.UtcNow,
            UserId = user.Id
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            User = MapToUserDto(user)
        };
    }

    private static UserDto MapToUserDto(ApplicationUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString()
        };
    }
}
