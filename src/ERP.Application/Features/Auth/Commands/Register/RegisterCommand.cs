using ERP.Application.Common.Models;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string Password,
    string Role) : IRequest<UserRegistrationResponse>;
