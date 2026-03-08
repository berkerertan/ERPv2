using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovements;
using MediatR;

namespace ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovementById;

public sealed class GetFinanceMovementByIdQueryHandler(IFinanceMovementRepository financeMovementRepository)
    : IRequestHandler<GetFinanceMovementByIdQuery, FinanceMovementDto>
{
    public async Task<FinanceMovementDto> Handle(GetFinanceMovementByIdQuery request, CancellationToken cancellationToken)
    {
        var movement = await financeMovementRepository.GetByIdAsync(request.FinanceMovementId, cancellationToken)
            ?? throw new NotFoundException("Finance movement not found.");

        return new FinanceMovementDto(
            movement.Id,
            movement.CariAccountId,
            movement.Type,
            movement.Amount,
            movement.MovementDateUtc,
            movement.Description,
            movement.ReferenceNo);
    }
}
