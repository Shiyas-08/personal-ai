using MediatR;
using PersonalAI.Application.Common.Interfaces;
using PersonalAI.Application.Features.Auth.DTOs;

namespace PersonalAI.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var loginDto = new LoginRequestDto
        {
            Email = request.Email,
            Password = request.Password
        };

        return await _authService.LoginAsync(loginDto);
    }
}
