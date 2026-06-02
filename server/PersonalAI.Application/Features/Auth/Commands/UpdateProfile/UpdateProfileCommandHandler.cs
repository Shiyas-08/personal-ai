using MediatR;
using PersonalAI.Application.Common.Interfaces;
using PersonalAI.Application.Features.Auth.DTOs;

namespace PersonalAI.Application.Features.Auth.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserDto>
{
    private readonly IAuthService _authService;

    public UpdateProfileCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var updateDto = new UpdateProfileRequestDto
        {
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        return await _authService.UpdateProfileAsync(request.UserId, updateDto);
    }
}
