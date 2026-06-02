using MediatR;
using PersonalAI.Application.Features.Auth.DTOs;

namespace PersonalAI.Application.Features.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand(
    string UserId,
    string FirstName,
    string LastName) : IRequest<UserDto>;
