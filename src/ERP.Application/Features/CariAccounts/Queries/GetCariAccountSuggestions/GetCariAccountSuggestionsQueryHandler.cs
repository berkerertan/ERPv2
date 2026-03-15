using ERP.Application.Abstractions.Persistence;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccountSuggestions;

public sealed class GetCariAccountSuggestionsQueryHandler(ICariAccountRepository repository)
    : IRequestHandler<GetCariAccountSuggestionsQuery, IReadOnlyList<CariAccountSuggestionDto>>
{
    public async Task<IReadOnlyList<CariAccountSuggestionDto>> Handle(GetCariAccountSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Search))
        {
            return [];
        }

        var search = request.Search.Trim();
        var limit = request.Limit <= 0 ? 8 : Math.Min(request.Limit, 20);

        IEnumerable<CariAccount> query = await repository.GetAllAsync(cancellationToken);

        query = query.Where(x =>
            x.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
            || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
            || (x.Phone != null && x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)));

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

        return query
            .OrderByDescending(x => x.Code.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(x => x.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Code)
            .Take(limit)
            .Select(x => new CariAccountSuggestionDto(
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                $"{x.Code} - {x.Name}",
                BuildSubtitle(x.Type, x.CurrentBalance, x.Phone)))
            .ToList();
    }

    private static string BuildSubtitle(CariType type, decimal currentBalance, string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return $"Tip: {type} | Bakiye: {currentBalance:N2}";
        }

        return $"Tip: {type} | Tel: {phone} | Bakiye: {currentBalance:N2}";
    }
}
