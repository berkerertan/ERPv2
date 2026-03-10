using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccountSuggestions;

public sealed record GetCariAccountSuggestionsQuery(
    string? Search,
    CariType? Type = null,
    int Limit = 8)
    : IRequest<IReadOnlyList<CariAccountSuggestionDto>>;
