using ERP.API.Common;
using ERP.API.Contracts.Products;
using ERP.Application.Abstractions.Media;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Features.Products.Commands.CreateProduct;
using ERP.Application.Features.Products.Commands.DeleteProduct;
using ERP.Application.Features.Products.Commands.UpdateProduct;
using ERP.Application.Features.Products.Queries.GetProductById;
using ERP.Application.Features.Products.Queries.GetProducts;
using ERP.Application.Features.Products.Queries.GetProductSuggestions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/products")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class ProductsController(
    IMediator mediator,
    ErpDbContext dbContext,
    IMediaStorageService mediaStorageService,
    IProductRepository productRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc",
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetProductsQuery(q, page, pageSize, sortBy, sortDir), cancellationToken);
        return Ok(response);
    }

    [HttpGet("suggest")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductSuggestionDto>>> Suggest(
        [FromQuery] string? q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetProductSuggestionsQuery(q, limit), cancellationToken);
        return Ok(response);
    }

    [HttpGet("scan")]
    [ProducesResponseType(typeof(ProductScanResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductScanResponse>> Scan(
        [FromQuery] string? barcode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return BadRequest("Barcode is required.");
        }

        var normalizedBarcode = barcode.Trim();
        var product = await productRepository.GetByBarcodeAsync(normalizedBarcode, cancellationToken);

        if (product is not null)
        {
            return Ok(new ProductScanResponse(
                normalizedBarcode,
                true,
                new ProductScanMatchDto(
                    product.Id,
                    product.Code,
                    product.Name,
                    product.BarcodeEan13,
                    product.QrCode,
                    product.DefaultSalePrice,
                    product.Unit,
                    product.ImageUrl,
                    product.IsActive),
                null));
        }

        var numericBarcode = normalizedBarcode.All(char.IsDigit) && normalizedBarcode.Length == 13
            ? normalizedBarcode
            : null;

        var qrCode = numericBarcode is null ? normalizedBarcode : null;
        var codeSuffix = normalizedBarcode.Length > 8
            ? normalizedBarcode[^8..]
            : normalizedBarcode;

        var safeSuffix = new string(codeSuffix.Where(char.IsLetterOrDigit).ToArray());
        var draftCode = string.IsNullOrWhiteSpace(safeSuffix)
            ? $"PRD-{DateTime.UtcNow:HHmmss}"
            : $"PRD-{safeSuffix.ToUpperInvariant()}";

        return Ok(new ProductScanResponse(
            normalizedBarcode,
            false,
            null,
            new ProductScanDraftDto(
                draftCode,
                $"Yeni Urun {normalizedBarcode}",
                "EA",
                "Genel",
                numericBarcode,
                qrCode,
                0m,
                1m)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateProductCommand(
            request.Code,
            request.Name,
            request.Unit,
            request.Category,
            request.ShortDescription,
            request.SubCategory,
            request.Brand,
            request.AlternativeUnits,
            request.BarcodeEan13,
            request.AlternativeBarcodes,
            request.QrCode,
            request.ProductType,
            request.PurchaseVatRate,
            request.SalesVatRate,
            request.IsActive,
            request.MinimumStockLevel,
            request.MaximumStockLevel,
            request.DefaultWarehouseId,
            request.DefaultShelfCode,
            request.ImageUrl,
            request.TechnicalDocumentUrl,
            request.LastPurchasePrice,
            request.LastSalePrice,
            request.DefaultSalePrice,
            request.CriticalStockLevel), cancellationToken);

        return Created($"/api/products/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateProductCommand(
            id,
            request.Code,
            request.Name,
            request.Unit,
            request.Category,
            request.ShortDescription,
            request.SubCategory,
            request.Brand,
            request.AlternativeUnits,
            request.BarcodeEan13,
            request.AlternativeBarcodes,
            request.QrCode,
            request.ProductType,
            request.PurchaseVatRate,
            request.SalesVatRate,
            request.IsActive,
            request.MinimumStockLevel,
            request.MaximumStockLevel,
            request.DefaultWarehouseId,
            request.DefaultShelfCode,
            request.ImageUrl,
            request.TechnicalDocumentUrl,
            request.LastPurchasePrice,
            request.LastSalePrice,
            request.DefaultSalePrice,
            request.CriticalStockLevel), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageUploadResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductImageUploadResponse>> UploadImage(
        Guid id,
        [FromForm] ProductImageUploadForm form,
        [FromQuery] bool deletePrevious = true,
        CancellationToken cancellationToken = default)
    {
        var file = form.File;

        if (!mediaStorageService.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Cloud media storage is not configured.");
        }

        if (file is null || file.Length <= 0)
        {
            return BadRequest("Image file is required.");
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only image files are allowed.");
        }

        const long maxBytes = 10 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            return BadRequest("Image file size cannot exceed 10 MB.");
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound("Product not found.");
        }

        var previousImageUrl = product.ImageUrl;

        await using var stream = file.OpenReadStream();
        var upload = await mediaStorageService.UploadProductImageAsync(stream, file.FileName, file.ContentType, cancellationToken);

        product.ImageUrl = upload.Url;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (deletePrevious)
        {
            var previousPublicId = mediaStorageService.TryExtractPublicIdFromUrl(previousImageUrl);
            if (!string.IsNullOrWhiteSpace(previousPublicId))
            {
                await mediaStorageService.DeleteByPublicIdAsync(previousPublicId, cancellationToken);
            }
        }

        return Ok(new ProductImageUploadResponse(
            product.Id,
            upload.Url,
            upload.PublicId,
            upload.Format,
            upload.Width,
            upload.Height,
            upload.Bytes));
    }

    [HttpDelete("{id:guid}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveImage(Guid id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound("Product not found.");
        }

        var existingImageUrl = product.ImageUrl;
        if (string.IsNullOrWhiteSpace(existingImageUrl))
        {
            return NoContent();
        }

        var publicId = mediaStorageService.TryExtractPublicIdFromUrl(existingImageUrl);
        if (!string.IsNullOrWhiteSpace(publicId))
        {
            await mediaStorageService.DeleteByPublicIdAsync(publicId, cancellationToken);
        }

        product.ImageUrl = null;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("bulk-price-update")]
    [ProducesResponseType(typeof(BulkProductPriceUpdateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkProductPriceUpdateResponse>> BulkPriceUpdate(
        [FromBody] BulkProductPriceUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest("At least one item is required.");
        }

        var normalizedItems = request.Items
            .GroupBy(x => x.ProductId)
            .Select(g => g.Last())
            .ToList();

        var ids = normalizedItems.Select(x => x.ProductId).ToList();
        var products = await dbContext.Products.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        var productLookup = products.ToDictionary(x => x.Id, x => x);

        var updatedCount = 0;
        var notFoundCount = 0;
        var now = DateTime.UtcNow;

        foreach (var item in normalizedItems)
        {
            if (!productLookup.TryGetValue(item.ProductId, out var product))
            {
                notFoundCount++;
                continue;
            }

            product.DefaultSalePrice = item.DefaultSalePrice;
            product.UpdatedAtUtc = now;
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new BulkProductPriceUpdateResponse(
            normalizedItems.Count,
            updatedCount,
            notFoundCount));
    }

    [HttpPost("bulk-stock-update")]
    [ProducesResponseType(typeof(BulkProductStockUpdateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkProductStockUpdateResponse>> BulkStockUpdate(
        [FromBody] BulkProductStockUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return BadRequest("At least one item is required.");
        }

        var warehouseExists = await dbContext.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
        {
            return BadRequest("Warehouse not found.");
        }

        var ids = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var productIds = products.ToHashSet();
        var movements = new List<StockMovement>();
        var notFoundCount = 0;
        var skippedZeroQuantity = 0;
        var movementDateUtc = request.MovementDateUtc ?? DateTime.UtcNow;

        foreach (var item in request.Items)
        {
            if (!productIds.Contains(item.ProductId))
            {
                notFoundCount++;
                continue;
            }

            if (item.QuantityDelta == 0)
            {
                skippedZeroQuantity++;
                continue;
            }

            movements.Add(new StockMovement
            {
                WarehouseId = request.WarehouseId,
                ProductId = item.ProductId,
                Type = item.QuantityDelta > 0 ? StockMovementType.In : StockMovementType.Out,
                Quantity = Math.Abs(item.QuantityDelta),
                UnitPrice = item.UnitPrice,
                MovementDateUtc = movementDateUtc,
                ReferenceNo = request.ReferenceNo
            });
        }

        if (movements.Count > 0)
        {
            dbContext.StockMovements.AddRange(movements);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new BulkProductStockUpdateResponse(
            request.Items.Count,
            movements.Count,
            notFoundCount,
            skippedZeroQuantity));
    }
}


