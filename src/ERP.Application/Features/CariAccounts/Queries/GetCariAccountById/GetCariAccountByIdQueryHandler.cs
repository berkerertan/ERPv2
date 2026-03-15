using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccountById;

public sealed class GetCariAccountByIdQueryHandler(ICariAccountRepository repository)
    : IRequestHandler<GetCariAccountByIdQuery, CariAccountDto>
{
    public async Task<CariAccountDto> Handle(GetCariAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(request.CariAccountId, cancellationToken)
            ?? throw new NotFoundException("Cari account not found.");

        return new CariAccountDto(
            account.Id,
            account.Code,
            account.Name,
            account.Phone,
            account.Type,
            account.RiskLimit,
            account.MaturityDays,
            account.CurrentBalance);
    }
}
