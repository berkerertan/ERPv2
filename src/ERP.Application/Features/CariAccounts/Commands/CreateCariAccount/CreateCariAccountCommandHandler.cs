using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Commands.CreateCariAccount;

public sealed class CreateCariAccountCommandHandler(ICariAccountRepository repository)
    : IRequestHandler<CreateCariAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateCariAccountCommand request, CancellationToken cancellationToken)
    {
        if (await repository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            throw new ConflictException("Cari code already exists.");
        }

        var account = new CariAccount
        {
            Code = request.Code,
            Name = request.Name,
            Phone = NormalizePhone(request.Phone),
            Type = request.Type,
            RiskLimit = request.RiskLimit,
            MaturityDays = request.MaturityDays,
            CurrentBalance = 0m
        };

        await repository.AddAsync(account, cancellationToken);
        return account.Id;
    }

    private static string? NormalizePhone(string? phone)
    {
        return string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
