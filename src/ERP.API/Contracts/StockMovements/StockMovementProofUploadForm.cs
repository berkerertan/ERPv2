using Microsoft.AspNetCore.Http;

namespace ERP.API.Contracts.StockMovements;

public sealed record StockMovementProofUploadForm(IFormFile? File);
