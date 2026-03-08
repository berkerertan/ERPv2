using Microsoft.AspNetCore.Http;

namespace ERP.API.Contracts.CariAccounts;

public sealed class ImportCariAccountsRequest
{
    public IFormFile File { get; set; } = default!;
    public bool UpsertExisting { get; set; } = true;

    // Optional dynamic column mapping
    public string? CodeColumn { get; set; }
    public string? NameColumn { get; set; }
    public string? TypeColumn { get; set; }
    public string? RiskLimitColumn { get; set; }
    public string? MaturityDaysColumn { get; set; }

    // Defaults when source file has no explicit type/code columns
    public string? DefaultType { get; set; } = "BuyerBch";
    public string? CodePrefix { get; set; } = "BCH";
}
