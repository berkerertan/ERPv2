using ERP.Domain.Enums;

namespace ERP.Application.Features.CariAccounts.Queries.GetCariAccountSuggestions;

public sealed record CariAccountSuggestionDto(
    Guid Id,
    string Code,
    string Name,
    CariType Type,
    string Label,
    string? Subtitle);
