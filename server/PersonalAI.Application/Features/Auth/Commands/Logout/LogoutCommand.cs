using MediatR;

namespace PersonalAI.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(string UserId, string RefreshToken) : IRequest;
