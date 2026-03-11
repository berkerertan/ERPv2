using ERP.Domain.Enums;

namespace ERP.API.Contracts.Auth;

public sealed record RegisterSaasRequest(
    string UserName,
    string Email,
    string Password,
    string CompanyName,
    SubscriptionPlan Plan = SubscriptionPlan.Starter);
