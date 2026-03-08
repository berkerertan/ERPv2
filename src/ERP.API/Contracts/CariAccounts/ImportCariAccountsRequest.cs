using Microsoft.AspNetCore.Http;

namespace ERP.API.Contracts.CariAccounts;

public sealed class ImportCariAccountsRequest
{
    public IFormFile File { get; set; } = default!;
    public bool UpsertExisting { get; set; } = true;
}
