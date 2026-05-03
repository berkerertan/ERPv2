using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.UpdateCariAccount;

public sealed record UpdateCariAccountCommand(
    Guid CariAccountId,
    string Code,
    string Name,
    CariType Type,
    decimal RiskLimit,
    int MaturityDays,
    int SupplierLeadTimeDays,
    string? Phone = null) : IRequest;
