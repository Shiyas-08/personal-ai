using PersonalAI.Application.Features.Auth.DTOs;

namespace PersonalAI.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task LogoutAsync(string userId, string refreshToken);
    Task<AuthResponseDto> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<UserDto> GetProfileAsync(string userId);
    Task<UserDto> UpdateProfileAsync(string userId, UpdateProfileRequestDto request);
}
