using ERP.Application.Common.Models;
using ERP.Domain.Enums;

namespace ERP.Application.Abstractions.Security;

public interface ISubscriptionPlanService
{
    Task<SubscriptionPlanConfig> GetPlanConfigAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionPlanConfig>> GetAllPlansAsync(bool onlyActive, CancellationToken cancellationToken = default);
}
