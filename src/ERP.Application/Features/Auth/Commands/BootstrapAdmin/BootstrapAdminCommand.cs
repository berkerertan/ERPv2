using ERP.Application.Common.Models;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.BootstrapAdmin;

public sealed record BootstrapAdminCommand(string UserName, string Email, string Password) : IRequest<AuthResponse>;
