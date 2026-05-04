using ERP.API.Common;
using ERP.API.Contracts.SupplierQuotes;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/supplier-quote-requests")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class SupplierQuoteRequestsController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierQuoteRequestListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierQuoteRequestListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var requests = await dbContext.SupplierQuoteRequests
            .AsNoTracking()
            .Include(x => x.Offers)
                .ThenInclude(x => x.Items)
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var warehouseLookup = await dbContext.Warehouses
            .AsNoTracking()
            .Where(x => requests.Select(r => r.WarehouseId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var rows = requests.Select(x => new SupplierQuoteRequestListItemDto(
            x.Id,
            x.RequestNo,
            x.Title,
            x.WarehouseId,
            warehouseLookup.GetValueOrDefault(x.WarehouseId) ?? "Depo",
            x.NeededByDateUtc,
            x.Status,
            x.Offers.Count,
            x.Offers.Count(o => o.Status == SupplierQuoteOfferStatus.Received),
            x.Offers
                .Where(o => o.Status == SupplierQuoteOfferStatus.Received)
                .Select(o => o.Items.Sum(i => i.OfferedQuantity * i.UnitPrice))
                .DefaultIfEmpty(0m)
                .Min(),
            x.CreatedByUserName,
            x.CreatedAtUtc)).ToList();

        return Ok(rows);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SupplierQuoteRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierQuoteRequestDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var request = await dbContext.SupplierQuoteRequests
            .Include(x => x.Items)
            .Include(x => x.Offers)
                .ThenInclude(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (request is null)
        {
            return NotFound();
        }

        var productIds = request.Items.Select(x => x.ProductId)
            .Concat(request.Offers.SelectMany(x => x.Items).Select(x => x.ProductId))
            .Distinct()
            .ToList();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code, x.Name, x.Unit })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var supplierIds = request.Offers.Select(x => x.SupplierCariAccountId).Distinct().ToList();
        var suppliers = await dbContext.CariAccounts
            .AsNoTracking()
            .Where(x => supplierIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var warehouseName = await dbContext.Warehouses
            .AsNoTracking()
            .Where(x => x.Id == request.WarehouseId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? "Depo";

        var dto = new SupplierQuoteRequestDetailDto(
            request.Id,
            request.RequestNo,
            request.Title,
            request.WarehouseId,
            warehouseName,
            request.NeededByDateUtc,
            request.Status,
            request.Notes,
            request.CreatedByUserName,
            request.CreatedAtUtc,
            request.SelectedSupplierCariAccountId,
            request.SelectedOfferId,
            request.Items.Select(item =>
            {
                var product = products[item.ProductId];
                return new SupplierQuoteRequestItemDto(item.ProductId, product.Code, product.Name, product.Unit, item.Quantity, item.TargetUnitPrice, item.Notes);
            }).ToList(),
            request.Offers
                .OrderByDescending(x => x.Id == request.SelectedOfferId)
                .ThenBy(x => x.Status)
                .Select(offer => new SupplierQuoteOfferDto(
                    offer.Id,
                    offer.SupplierCariAccountId,
                    suppliers.GetValueOrDefault(offer.SupplierCariAccountId) ?? "Tedarikci",
                    offer.Status,
                    offer.LeadTimeDays,
                    offer.Notes,
                    offer.RespondedAtUtc,
                    offer.Items.Sum(i => i.OfferedQuantity * i.UnitPrice),
                    offer.Id == request.SelectedOfferId,
                    offer.Items.Select(item =>
                    {
                        var product = products[item.ProductId];
                        return new SupplierQuoteOfferItemDto(item.ProductId, product.Code, product.Name, item.OfferedQuantity, item.UnitPrice, item.MinimumOrderQuantity, item.OfferedQuantity * item.UnitPrice);
                    }).ToList()))
                .ToList());

        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSupplierQuoteRequestRequest request, CancellationToken cancellationToken)
    {
        if (request.SupplierCariAccountIds.Count == 0)
        {
            return BadRequest("En az bir tedarikci secin.");
        }

        if (request.Items.Count == 0)
        {
            return BadRequest("En az bir urun ekleyin.");
        }

        var entity = new SupplierQuoteRequest
        {
            RequestNo = await GenerateRequestNoAsync(cancellationToken),
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Tedarikci teklif talebi" : request.Title.Trim(),
            WarehouseId = request.WarehouseId,
            NeededByDateUtc = request.NeededByDateUtc,
            Notes = request.Notes?.Trim(),
            CreatedByUserName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name,
            Status = SupplierQuoteRequestStatus.Open,
            Items = request.Items.Select(x => new SupplierQuoteRequestItem
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity,
                TargetUnitPrice = x.TargetUnitPrice,
                Notes = x.Notes?.Trim()
            }).ToList(),
            Offers = request.SupplierCariAccountIds.Distinct().Select(supplierId => new SupplierQuoteOffer
            {
                SupplierCariAccountId = supplierId,
                Status = SupplierQuoteOfferStatus.Pending
            }).ToList()
        };

        dbContext.SupplierQuoteRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/supplier-quote-requests/{entity.Id}", entity.Id);
    }

    [HttpPut("{id:guid}/offers/{offerId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpsertOffer(Guid id, Guid offerId, [FromBody] UpsertSupplierQuoteOfferRequest request, CancellationToken cancellationToken)
    {
        var offer = await dbContext.SupplierQuoteOffers
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == offerId && x.SupplierQuoteRequestId == id, cancellationToken);

        if (offer is null)
        {
            return NotFound();
        }

        offer.Status = request.Status;
        offer.LeadTimeDays = request.LeadTimeDays;
        offer.Notes = request.Notes?.Trim();
        offer.RespondedAtUtc = request.Status == SupplierQuoteOfferStatus.Pending ? null : DateTime.UtcNow;

        dbContext.SupplierQuoteOfferItems.RemoveRange(offer.Items);
        offer.Items = request.Items.Select(x => new SupplierQuoteOfferItem
        {
            ProductId = x.ProductId,
            OfferedQuantity = x.OfferedQuantity,
            UnitPrice = x.UnitPrice,
            MinimumOrderQuantity = x.MinimumOrderQuantity
        }).ToList();

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/select-offer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SelectOffer(Guid id, [FromBody] SelectSupplierQuoteOfferRequest request, CancellationToken cancellationToken)
    {
        var quoteRequest = await dbContext.SupplierQuoteRequests
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (quoteRequest is null)
        {
            return NotFound();
        }

        var offer = await dbContext.SupplierQuoteOffers
            .FirstOrDefaultAsync(x => x.Id == request.OfferId && x.SupplierQuoteRequestId == id, cancellationToken);
        if (offer is null)
        {
            return NotFound();
        }

        quoteRequest.SelectedOfferId = offer.Id;
        quoteRequest.SelectedSupplierCariAccountId = offer.SupplierCariAccountId;
        quoteRequest.Status = SupplierQuoteRequestStatus.Closed;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/convert-selected-offer")]
    [ProducesResponseType(typeof(ConvertSupplierQuoteResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConvertSupplierQuoteResultDto>> ConvertSelectedOffer(Guid id, CancellationToken cancellationToken)
    {
        var quoteRequest = await dbContext.SupplierQuoteRequests
            .Include(x => x.Offers)
                .ThenInclude(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (quoteRequest is null)
        {
            return NotFound();
        }

        if (!quoteRequest.SelectedOfferId.HasValue)
        {
            return BadRequest("Once bir teklif secin.");
        }

        var offer = quoteRequest.Offers.FirstOrDefault(x => x.Id == quoteRequest.SelectedOfferId.Value);
        if (offer is null || offer.Items.Count == 0)
        {
            return BadRequest("Secili teklifta urun yok.");
        }

        var orderNo = await GeneratePurchaseOrderNoAsync(cancellationToken);
        var purchaseOrder = new PurchaseOrder
        {
            OrderNo = orderNo,
            SupplierCariAccountId = offer.SupplierCariAccountId,
            WarehouseId = quoteRequest.WarehouseId,
            Status = OrderStatus.Draft,
            Items = offer.Items.Select(x => new PurchaseOrderItem
            {
                ProductId = x.ProductId,
                Quantity = x.OfferedQuantity,
                UnitPrice = x.UnitPrice
            }).ToList()
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        quoteRequest.Status = SupplierQuoteRequestStatus.Converted;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ConvertSupplierQuoteResultDto(purchaseOrder.Id, orderNo));
    }

    private async Task<string> GenerateRequestNoAsync(CancellationToken cancellationToken)
    {
        var prefix = $"TT-{DateTime.UtcNow:yyyyMMdd}";
        var count = await dbContext.SupplierQuoteRequests.CountAsync(x => x.RequestNo.StartsWith(prefix), cancellationToken);
        return $"{prefix}-{count + 1:000}";
    }

    private async Task<string> GeneratePurchaseOrderNoAsync(CancellationToken cancellationToken)
    {
        var prefix = $"AS-{DateTime.UtcNow:yyyy}";
        var count = await dbContext.PurchaseOrders.CountAsync(x => x.OrderNo.StartsWith(prefix), cancellationToken);
        return $"{prefix}-{count + 1:000}";
    }
}
