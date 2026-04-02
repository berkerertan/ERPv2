using ERP.API.Common;
using ERP.API.Contracts.DocumentScanner;
using ERP.Application.Abstractions.DocumentScanner;
using ERP.Application.Features.CariAccounts.Commands.CreateCariDebtItem;
using ERP.Application.Features.Products.Commands.CreateProduct;
using ERP.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;
using ERP.Application.Features.StockMovements.Commands.CreateStockMovement;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class DocumentScannerController(
    IDocumentScannerService documentScannerService,
    IMediator mediator,
    ErpDbContext dbContext) : ControllerBase
{
    private sealed record NormalizedScanLineItem(
        string Description,
        decimal Quantity,
        string Unit,
        decimal UnitPrice,
        decimal TotalPrice);

    private sealed record ResolvedProductLineItem(Guid ProductId, decimal Quantity, decimal UnitPrice);

    [HttpPost("Analyze")]
    [ProducesResponseType(typeof(DocumentScanResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentScanResult>> Analyze(
        [FromBody] ScanDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return BadRequest("ImageBase64 is required.");
        }

        if (string.IsNullOrWhiteSpace(request.MimeType))
        {
            return BadRequest("MimeType is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return BadRequest("Provider is required.");
        }

        var result = await documentScannerService.AnalyzeAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("Commit")]
    [ProducesResponseType(typeof(CommitScannedDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommitScannedDocumentResponse>> Commit(
        [FromBody] CommitScannedDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var operation = NormalizeOperation(request.Operation);
        if (operation is null)
        {
            return BadRequest(
                $"Operation must be one of: {DocumentScanCommitOperations.BuyerDebt}, {DocumentScanCommitOperations.SupplierPurchaseOrder}, {DocumentScanCommitOperations.StockIn}.");
        }

        var normalizedItems = NormalizeItems(request.Items);
        if (normalizedItems.Count == 0)
        {
            return BadRequest("At least one valid line item is required.");
        }

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var response = operation switch
            {
                DocumentScanCommitOperations.BuyerDebt => await CommitBuyerDebtAsync(request, normalizedItems, cancellationToken),
                DocumentScanCommitOperations.SupplierPurchaseOrder => await CommitSupplierPurchaseOrderAsync(request, normalizedItems, cancellationToken),
                DocumentScanCommitOperations.StockIn => await CommitStockInAsync(request, normalizedItems, cancellationToken),
                _ => throw new ArgumentException("Unsupported operation.")
            };

            await transaction.CommitAsync(cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private async Task<CommitScannedDocumentResponse> CommitBuyerDebtAsync(
        CommitScannedDocumentRequest request,
        IReadOnlyList<NormalizedScanLineItem> items,
        CancellationToken cancellationToken)
    {
        if (!request.BuyerCariAccountId.HasValue || request.BuyerCariAccountId.Value == Guid.Empty)
        {
            throw new ArgumentException("BuyerCariAccountId is required for buyerDebt operation.");
        }

        var buyer = await dbContext.CariAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.BuyerCariAccountId, cancellationToken)
            ?? throw new ArgumentException("Selected buyer cari account could not be found.");

        if (buyer.Type == CariType.Supplier)
        {
            throw new ArgumentException("Selected cari account is not a buyer.");
        }

        var transactionDate = ParseTransactionDate(request.DocumentDate);
        var runningBalance = buyer.CurrentBalance;
        var createdDebtItemIds = new List<Guid>(items.Count);

        foreach (var item in items)
        {
            var materialDescription = TrimToLength(item.Description, 250);
            var lineTotal = item.TotalPrice > 0 ? item.TotalPrice : item.Quantity * item.UnitPrice;
            runningBalance += lineTotal;

            var debtItemId = await mediator.Send(
                new CreateCariDebtItemCommand(
                    buyer.Id,
                    transactionDate,
                    materialDescription,
                    item.Quantity,
                    item.UnitPrice,
                    item.UnitPrice,
                    lineTotal,
                    0m,
                    runningBalance),
                cancellationToken);

            createdDebtItemIds.Add(debtItemId);
        }

        return new CommitScannedDocumentResponse
        {
            Operation = DocumentScanCommitOperations.BuyerDebt,
            SourceItemCount = request.Items.Count,
            ProcessedItemCount = items.Count,
            CreatedProductCount = 0,
            CreatedProductIds = [],
            CreatedRecordIds = createdDebtItemIds,
            Message = $"{createdDebtItemIds.Count} borc kalemi secilen aliciya eklendi."
        };
    }

    private async Task<CommitScannedDocumentResponse> CommitSupplierPurchaseOrderAsync(
        CommitScannedDocumentRequest request,
        IReadOnlyList<NormalizedScanLineItem> items,
        CancellationToken cancellationToken)
    {
        if (!request.SupplierCariAccountId.HasValue || request.SupplierCariAccountId.Value == Guid.Empty)
        {
            throw new ArgumentException("SupplierCariAccountId is required for supplierPurchaseOrder operation.");
        }

        if (!request.WarehouseId.HasValue || request.WarehouseId.Value == Guid.Empty)
        {
            throw new ArgumentException("WarehouseId is required for supplierPurchaseOrder operation.");
        }

        var supplier = await dbContext.CariAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.SupplierCariAccountId, cancellationToken)
            ?? throw new ArgumentException("Selected supplier cari account could not be found.");

        if (supplier.Type == CariType.BuyerBch)
        {
            throw new ArgumentException("Selected cari account is not a supplier.");
        }

        var warehouseExists = await dbContext.Warehouses
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.WarehouseId, cancellationToken);

        if (!warehouseExists)
        {
            throw new ArgumentException("Selected warehouse could not be found.");
        }

        var (resolvedItems, createdProductIds) = await ResolveProductsAsync(items, request.CreateMissingProducts, cancellationToken);

        var orderNo = await GenerateUniquePurchaseOrderNoAsync(request.DocumentNumber, cancellationToken);
        var purchaseOrderId = await mediator.Send(
            new CreatePurchaseOrderCommand(
                orderNo,
                request.SupplierCariAccountId.Value,
                request.WarehouseId.Value,
                resolvedItems.Select(x => new CreatePurchaseOrderItemInput(x.ProductId, x.Quantity, x.UnitPrice)).ToList()),
            cancellationToken);

        return new CommitScannedDocumentResponse
        {
            Operation = DocumentScanCommitOperations.SupplierPurchaseOrder,
            SourceItemCount = request.Items.Count,
            ProcessedItemCount = resolvedItems.Count,
            CreatedProductCount = createdProductIds.Count,
            CreatedProductIds = createdProductIds,
            CreatedRecordIds = [purchaseOrderId],
            Message = "Taranan kalemlerden satin alma siparisi olusturuldu."
        };
    }

    private async Task<CommitScannedDocumentResponse> CommitStockInAsync(
        CommitScannedDocumentRequest request,
        IReadOnlyList<NormalizedScanLineItem> items,
        CancellationToken cancellationToken)
    {
        if (!request.WarehouseId.HasValue || request.WarehouseId.Value == Guid.Empty)
        {
            throw new ArgumentException("WarehouseId is required for stockIn operation.");
        }

        var warehouseExists = await dbContext.Warehouses
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.WarehouseId, cancellationToken);

        if (!warehouseExists)
        {
            throw new ArgumentException("Selected warehouse could not be found.");
        }

        var (resolvedItems, createdProductIds) = await ResolveProductsAsync(items, request.CreateMissingProducts, cancellationToken);
        var createdMovementIds = new List<Guid>(resolvedItems.Count);
        var referenceNo = BuildReferenceNo(request.DocumentNumber, "SCAN-IN");

        foreach (var item in resolvedItems)
        {
            var movementId = await mediator.Send(
                new CreateStockMovementCommand(
                    request.WarehouseId.Value,
                    item.ProductId,
                    StockMovementType.In,
                    item.Quantity,
                    item.UnitPrice,
                    referenceNo,
                    StockMovementReason.InventoryAdjustment),
                cancellationToken);

            createdMovementIds.Add(movementId);
        }

        return new CommitScannedDocumentResponse
        {
            Operation = DocumentScanCommitOperations.StockIn,
            SourceItemCount = request.Items.Count,
            ProcessedItemCount = resolvedItems.Count,
            CreatedProductCount = createdProductIds.Count,
            CreatedProductIds = createdProductIds,
            CreatedRecordIds = createdMovementIds,
            Message = $"{createdMovementIds.Count} adet stok giris hareketi olusturuldu."
        };
    }

    private async Task<(IReadOnlyList<ResolvedProductLineItem> ResolvedItems, IReadOnlyList<Guid> CreatedProductIds)> ResolveProductsAsync(
        IReadOnlyList<NormalizedScanLineItem> items,
        bool createMissingProducts,
        CancellationToken cancellationToken)
    {
        var resolvedItems = new List<ResolvedProductLineItem>(items.Count);
        var createdProductIds = new List<Guid>();
        var cache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (cache.TryGetValue(item.Description, out var cachedProductId))
            {
                resolvedItems.Add(new ResolvedProductLineItem(cachedProductId, item.Quantity, item.UnitPrice));
                continue;
            }

            var productId = await TryFindProductIdAsync(item.Description, cancellationToken);
            if (productId is null)
            {
                if (!createMissingProducts)
                {
                    throw new ArgumentException(
                        $"Product could not be resolved for line '{item.Description}'. Enable createMissingProducts or edit line description.");
                }

                productId = await CreateProductFromScanLineAsync(item, cancellationToken);
                createdProductIds.Add(productId.Value);
            }

            cache[item.Description] = productId.Value;
            resolvedItems.Add(new ResolvedProductLineItem(productId.Value, item.Quantity, item.UnitPrice));
        }

        return (resolvedItems, createdProductIds);
    }

    private async Task<Guid?> TryFindProductIdAsync(string description, CancellationToken cancellationToken)
    {
        var exactMatch = await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Name == description || x.Code == description)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (exactMatch is not null)
        {
            return exactMatch.Value;
        }

        var normalized = description.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Name.Contains(normalized))
            .OrderBy(x => x.Name.Length)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> CreateProductFromScanLineAsync(NormalizedScanLineItem item, CancellationToken cancellationToken)
    {
        var productName = TrimToLength(item.Description, 200);
        var code = await GenerateUniqueProductCodeAsync(productName, cancellationToken);
        var unit = NormalizeUnit(item.Unit);

        return await mediator.Send(
            new CreateProductCommand(
                code,
                productName,
                unit,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                item.UnitPrice > 0 ? item.UnitPrice : null,
                null,
                item.UnitPrice,
                0m),
            cancellationToken);
    }

    private async Task<string> GenerateUniqueProductCodeAsync(string description, CancellationToken cancellationToken)
    {
        var token = NormalizeCodeToken(description);
        if (string.IsNullOrWhiteSpace(token))
        {
            token = "ITEM";
        }

        token = token.Length > 20 ? token[..20] : token;
        var baseCode = $"SCN-{token}";
        var candidate = baseCode.Length > 30 ? baseCode[..30] : baseCode;
        var sequence = 1;

        while (await dbContext.Products.AsNoTracking().AnyAsync(x => x.Code == candidate, cancellationToken))
        {
            sequence++;
            var suffix = $"-{sequence}";
            var maxPrefixLength = Math.Max(1, 30 - suffix.Length);
            var prefix = baseCode.Length > maxPrefixLength ? baseCode[..maxPrefixLength] : baseCode;
            candidate = $"{prefix}{suffix}";
        }

        return candidate;
    }

    private async Task<string> GenerateUniquePurchaseOrderNoAsync(string? documentNumber, CancellationToken cancellationToken)
    {
        var token = NormalizeCodeToken(documentNumber ?? string.Empty);
        if (string.IsNullOrWhiteSpace(token))
        {
            token = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        }

        var seed = $"PO-{token}";
        seed = seed.Length > 30 ? seed[..30] : seed;

        var candidate = seed;
        var sequence = 1;

        while (await dbContext.PurchaseOrders.AsNoTracking().AnyAsync(x => x.OrderNo == candidate, cancellationToken))
        {
            sequence++;
            var suffix = $"-{sequence}";
            var maxPrefixLength = Math.Max(1, 30 - suffix.Length);
            candidate = $"{seed[..Math.Min(seed.Length, maxPrefixLength)]}{suffix}";
        }

        return candidate;
    }

    private static IReadOnlyList<NormalizedScanLineItem> NormalizeItems(IReadOnlyList<CommitScannedLineItemRequest>? items)
    {
        if (items is null || items.Count == 0)
        {
            return [];
        }

        var normalized = new List<NormalizedScanLineItem>(items.Count);
        foreach (var item in items)
        {
            var description = item.Description?.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            var quantity = item.Quantity > 0 ? item.Quantity : 1m;
            var unitPrice = item.UnitPrice < 0 ? 0m : item.UnitPrice;
            var totalPrice = item.TotalPrice < 0 ? 0m : item.TotalPrice;
            if (totalPrice <= 0)
            {
                totalPrice = quantity * unitPrice;
            }

            normalized.Add(
                new NormalizedScanLineItem(
                    TrimToLength(description, 250),
                    quantity,
                    item.Unit,
                    unitPrice,
                    totalPrice));
        }

        return normalized;
    }

    private static string? NormalizeOperation(string? operation)
    {
        if (string.IsNullOrWhiteSpace(operation))
        {
            return null;
        }

        var normalized = operation.Trim().ToLowerInvariant();
        return normalized switch
        {
            "buyerdebt" => DocumentScanCommitOperations.BuyerDebt,
            "supplierpurchaseorder" => DocumentScanCommitOperations.SupplierPurchaseOrder,
            "stockin" => DocumentScanCommitOperations.StockIn,
            _ => null
        };
    }

    private static DateTime ParseTransactionDate(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.AssumeLocal, out var parsedTr))
            {
                return parsedTr.Date;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedInvariant))
            {
                return parsedInvariant.Date;
            }
        }

        return DateTime.UtcNow.Date;
    }

    private static string BuildReferenceNo(string? documentNumber, string fallbackPrefix)
    {
        var raw = string.IsNullOrWhiteSpace(documentNumber)
            ? $"{fallbackPrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : documentNumber.Trim();

        return raw.Length <= 50 ? raw : raw[..50];
    }

    private static string NormalizeUnit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "EA";
        }

        var unit = value.Trim().ToUpperInvariant();
        return unit.Length <= 10 ? unit : unit[..10];
    }

    private static string NormalizeCodeToken(string value)
    {
        var upper = value.Trim().ToUpperInvariant();
        var sb = new StringBuilder(upper.Length);

        foreach (var ch in upper)
        {
            if (ch is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
