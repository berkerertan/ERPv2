using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;

public sealed class GetCariAccountsQueryHandler(ICariAccountRepository repository)
    : IRequestHandler<GetCariAccountsQuery, IReadOnlyList<CariAccountDto>>
{
    public async Task<IReadOnlyList<CariAccountDto>> Handle(
        GetCariAccountsQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await repository.GetAllAsync(cancellationToken);
        IEnumerable<CariAccount> query = accounts;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (request.Type.HasValue)
        {
            query = request.Type.Value switch
            {
                CariType.Supplier => query.Where(x => x.Type == CariType.Supplier || x.Type == CariType.Both),
                CariType.BuyerBch => query.Where(x => x.Type == CariType.BuyerBch || x.Type == CariType.Both),
                CariType.Both => query.Where(x => x.Type == CariType.Both),
                _ => query
            };
        }

        query = ApplySort(query, request.SortBy, request.SortDir);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200);

        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CariAccountDto(
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.RiskLimit,
                x.MaturityDays,
                x.CurrentBalance))
            .ToList();
    }

    private static IEnumerable<CariAccount> ApplySort(IEnumerable<CariAccount> query, string? sortBy, string sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var key = sortBy?.Trim().ToLowerInvariant();

        return key switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "balance" or "currentbalance" => desc ? query.OrderByDescending(x => x.CurrentBalance) : query.OrderBy(x => x.CurrentBalance),
            "risk" or "risklimit" => desc ? query.OrderByDescending(x => x.RiskLimit) : query.OrderBy(x => x.RiskLimit),
            _ => desc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code)
        };
    }
}
