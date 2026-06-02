using MediatR;
using PersonalAI.Application.Features.Auth.DTOs;

namespace PersonalAI.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName) : IRequest<AuthResponseDto>;
