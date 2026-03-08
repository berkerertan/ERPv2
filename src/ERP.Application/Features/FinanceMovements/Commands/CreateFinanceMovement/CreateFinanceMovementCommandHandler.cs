using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.FinanceMovements.Commands.CreateFinanceMovement;

public sealed class CreateFinanceMovementCommandHandler(
    ICariAccountRepository cariAccountRepository,
    IFinanceMovementRepository financeMovementRepository)
    : IRequestHandler<CreateFinanceMovementCommand, Guid>
{
    public async Task<Guid> Handle(CreateFinanceMovementCommand request, CancellationToken cancellationToken)
    {
        var cari = await cariAccountRepository.GetByIdAsync(request.CariAccountId, cancellationToken)
            ?? throw new NotFoundException("Cari account not found.");

        if (request.Type == FinanceMovementType.Collection && cari.Type == CariType.Supplier)
        {
            throw new ConflictException("Collection can only be used for buyer/BCH or both cari types.");
        }

        if (request.Type == FinanceMovementType.Payment && cari.Type == CariType.BuyerBch)
        {
            throw new ConflictException("Payment can only be used for supplier/both cari types.");
        }

        if (request.Type == FinanceMovementType.Collection)
        {
            cari.CurrentBalance -= request.Amount;
        }
        else
        {
            cari.CurrentBalance += request.Amount;
        }

        await cariAccountRepository.UpdateAsync(cari, cancellationToken);

        var movement = new FinanceMovement
        {
            CariAccountId = request.CariAccountId,
            Type = request.Type,
            Amount = request.Amount,
            Description = request.Description,
            ReferenceNo = request.ReferenceNo,
            MovementDateUtc = DateTime.UtcNow
        };

        await financeMovementRepository.AddAsync(movement, cancellationToken);
        return movement.Id;
    }
}
