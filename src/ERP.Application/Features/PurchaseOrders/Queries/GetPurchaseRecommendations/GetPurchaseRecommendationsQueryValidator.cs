using FluentValidation;

namespace ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendations;

public sealed class GetPurchaseRecommendationsQueryValidator : AbstractValidator<GetPurchaseRecommendationsQuery>
{
    public GetPurchaseRecommendationsQueryValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.AnalysisDays).InclusiveBetween(7, 120);
        RuleFor(x => x.CoverageDays).InclusiveBetween(7, 90);
        RuleFor(x => x.MaxItems).InclusiveBetween(5, 100);
    }
}
