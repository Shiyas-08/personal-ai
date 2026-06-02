using MediatR;
using PersonalAI.Application.Features.Auth.DTOs;

namespace PersonalAI.Application.Features.Auth.Queries.GetProfile;

public record GetProfileQuery(string UserId) : IRequest<UserDto>;
