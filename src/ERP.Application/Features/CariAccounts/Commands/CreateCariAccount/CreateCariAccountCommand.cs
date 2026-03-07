using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.CreateCariAccount;

public sealed record CreateCariAccountCommand(
    string Code,
    string Name,
    CariType Type,
    decimal RiskLimit,
    int MaturityDays) : IRequest<Guid>;
