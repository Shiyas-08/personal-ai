using MediatR;
using PersonalAI.Application.Common.Interfaces;
using PersonalAI.Application.Features.Auth.DTOs;

namespace PersonalAI.Application.Features.Auth.Queries.GetProfile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserDto>
{
    private readonly IAuthService _authService;

    public GetProfileQueryHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<UserDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        return await _authService.GetProfileAsync(request.UserId);
    }
}
