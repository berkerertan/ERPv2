using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.FinanceMovements.Commands.UpdateFinanceMovement;

public sealed class UpdateFinanceMovementCommandHandler(
    ICariAccountRepository cariAccountRepository,
    IFinanceMovementRepository financeMovementRepository)
    : IRequestHandler<UpdateFinanceMovementCommand>
{
    public async Task Handle(UpdateFinanceMovementCommand request, CancellationToken cancellationToken)
    {
        var movement = await financeMovementRepository.GetByIdAsync(request.FinanceMovementId, cancellationToken)
            ?? throw new NotFoundException("Finance movement not found.");

        var targetCari = await cariAccountRepository.GetByIdAsync(request.CariAccountId, cancellationToken)
            ?? throw new NotFoundException("Cari account not found.");

        if (request.Type == FinanceMovementType.Collection && targetCari.Type == CariType.Supplier)
        {
            throw new ConflictException("Collection can only be used for buyer/BCH or both cari types.");
        }

        if (request.Type == FinanceMovementType.Payment && targetCari.Type == CariType.BuyerBch)
        {
            throw new ConflictException("Payment can only be used for supplier/both cari types.");
        }

        var oldDelta = CalculateDelta(movement.Type, movement.Amount);
        var newDelta = CalculateDelta(request.Type, request.Amount);

        if (movement.CariAccountId == request.CariAccountId)
        {
            targetCari.CurrentBalance += newDelta - oldDelta;
            await cariAccountRepository.UpdateAsync(targetCari, cancellationToken);
        }
        else
        {
            var previousCari = await cariAccountRepository.GetByIdAsync(movement.CariAccountId, cancellationToken)
                ?? throw new NotFoundException("Previous cari account not found.");

            previousCari.CurrentBalance -= oldDelta;
            targetCari.CurrentBalance += newDelta;

            await cariAccountRepository.UpdateAsync(previousCari, cancellationToken);
            await cariAccountRepository.UpdateAsync(targetCari, cancellationToken);
        }

        movement.CariAccountId = request.CariAccountId;
        movement.Type = request.Type;
        movement.Amount = request.Amount;
        movement.Description = request.Description;
        movement.ReferenceNo = request.ReferenceNo;

        await financeMovementRepository.UpdateAsync(movement, cancellationToken);
    }

    private static decimal CalculateDelta(FinanceMovementType type, decimal amount)
    {
        return type == FinanceMovementType.Collection ? -amount : amount;
    }
}
