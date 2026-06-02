using MediatR;
using PersonalAI.Application.Common.Interfaces;
using PersonalAI.Application.Features.Auth.DTOs;

namespace PersonalAI.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var registerDto = new RegisterRequestDto
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        return await _authService.RegisterAsync(registerDto);
    }
}
