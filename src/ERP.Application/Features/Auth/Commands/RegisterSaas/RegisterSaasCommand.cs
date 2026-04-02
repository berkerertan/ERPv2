using ERP.Application.Common.Models;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.Auth.Commands.RegisterSaas;

public sealed record RegisterSaasCommand(
    string UserName,
    string Email,
    string Password,
    string CompanyName,
    SubscriptionPlan Plan) : IRequest<UserRegistrationResponse>;
