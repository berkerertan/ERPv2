using ERP.Domain.Enums;

namespace ERP.Application.Common.Models;

public sealed record SubscriptionPlanConfig(
    SubscriptionPlan Plan,
    string DisplayName,
    string AssignedRole,
    decimal MonthlyPrice,
    int MaxUsers,
    IReadOnlyList<string> Features,
    bool IsActive);
