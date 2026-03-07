using ERP.Application.Common.Models;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string UserName, string Password) : IRequest<AuthResponse>;
