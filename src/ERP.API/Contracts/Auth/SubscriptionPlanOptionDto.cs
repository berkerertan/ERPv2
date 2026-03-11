using ERP.Domain.Enums;

namespace ERP.API.Contracts.Auth;

public sealed record SubscriptionPlanOptionDto(
    SubscriptionPlan Plan,
    string Name,
    string AssignedRole,
    decimal MonthlyPrice,
    int MaxUsers,
    bool IsActive,
    IReadOnlyList<string> Features);
