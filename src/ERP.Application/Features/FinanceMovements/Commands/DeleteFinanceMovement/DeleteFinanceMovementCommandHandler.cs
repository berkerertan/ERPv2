using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.FinanceMovements.Commands.DeleteFinanceMovement;

public sealed class DeleteFinanceMovementCommandHandler(
    IFinanceMovementRepository financeMovementRepository,
    ICariAccountRepository cariAccountRepository)
    : IRequestHandler<DeleteFinanceMovementCommand>
{
    public async Task Handle(DeleteFinanceMovementCommand request, CancellationToken cancellationToken)
    {
        var movement = await financeMovementRepository.GetByIdAsync(request.FinanceMovementId, cancellationToken)
            ?? throw new NotFoundException("Finance movement not found.");

        var cari = await cariAccountRepository.GetByIdAsync(movement.CariAccountId, cancellationToken)
            ?? throw new NotFoundException("Cari account not found.");

        var delta = movement.Type == FinanceMovementType.Collection
            ? movement.Amount
            : -movement.Amount;

        cari.CurrentBalance += delta;
        await cariAccountRepository.UpdateAsync(cari, cancellationToken);

        await financeMovementRepository.DeleteAsync(movement.Id, cancellationToken);
    }
}
