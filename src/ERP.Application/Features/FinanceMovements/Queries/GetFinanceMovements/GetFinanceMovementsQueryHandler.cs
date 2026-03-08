using ERP.Application.Abstractions.Persistence;
using MediatR;

namespace ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovements;

public sealed class GetFinanceMovementsQueryHandler(IFinanceMovementRepository financeMovementRepository)
    : IRequestHandler<GetFinanceMovementsQuery, IReadOnlyList<FinanceMovementDto>>
{
    public async Task<IReadOnlyList<FinanceMovementDto>> Handle(GetFinanceMovementsQuery request, CancellationToken cancellationToken)
    {
        var movements = await financeMovementRepository.GetAllAsync(cancellationToken);

        return movements
            .OrderByDescending(x => x.MovementDateUtc)
            .Select(x => new FinanceMovementDto(
                x.Id,
                x.CariAccountId,
                x.Type,
                x.Amount,
                x.MovementDateUtc,
                x.Description,
                x.ReferenceNo))
            .ToList();
    }
}
