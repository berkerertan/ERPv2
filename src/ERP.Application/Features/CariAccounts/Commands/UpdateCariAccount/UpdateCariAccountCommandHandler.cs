using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.UpdateCariAccount;

public sealed class UpdateCariAccountCommandHandler(ICariAccountRepository repository)
    : IRequestHandler<UpdateCariAccountCommand>
{
    public async Task Handle(UpdateCariAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(request.CariAccountId, cancellationToken)
            ?? throw new NotFoundException("Cari account not found.");

        var codeOwner = await repository.GetByCodeAsync(request.Code, cancellationToken);
        if (codeOwner is not null && codeOwner.Id != account.Id)
        {
            throw new ConflictException("Cari account code already exists.");
        }

        account.Code = request.Code;
        account.Name = request.Name;
        account.Phone = NormalizePhone(request.Phone);
        account.Type = request.Type;
        account.RiskLimit = request.RiskLimit;
        account.MaturityDays = request.MaturityDays;
        account.SupplierLeadTimeDays = request.SupplierLeadTimeDays;

        await repository.UpdateAsync(account, cancellationToken);
    }

    private static string? NormalizePhone(string? phone)
    {
        return string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
