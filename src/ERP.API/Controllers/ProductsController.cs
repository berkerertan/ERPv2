using ERP.API.Common;
using ERP.API.Contracts.Products;
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
public sealed class ProductsController(IMediator mediator, ErpDbContext dbContext) : ControllerBase
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


