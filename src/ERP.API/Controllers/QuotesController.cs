using ERP.API.Common;
using ERP.API.Contracts.Quotes;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/quotes")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class QuotesController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<QuoteListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QuoteListDto>>> GetQuotes(
        [FromQuery] string? q,
        [FromQuery] QuoteStatus? status,
        [FromQuery] Guid? cariAccountId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Quotes.AsNoTracking().Include(x => x.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.QuoteNumber.ToLower().Contains(term) ||
                x.CustomerName.ToLower().Contains(term));
        }

        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (cariAccountId.HasValue) query = query.Where(x => x.CariAccountId == cariAccountId.Value);

        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = rows.Select(x =>
        {
            var grandTotal = CalculateGrandTotal(x);
            return new QuoteListDto(
                x.Id, x.QuoteNumber, x.CustomerName, x.Status,
                x.QuoteDateUtc, x.ValidUntilUtc, x.Items.Count,
                grandTotal, x.CreatedAtUtc);
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<QuoteDto>> GetQuoteById(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Quotes.AsNoTracking()
            .Include(x => x.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (row is null) return NotFound();

        return Ok(await MapToDto(row, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateQuote([FromBody] UpsertQuoteRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request);
        if (validation is not null) return BadRequest(validation);

        var quoteNumber = request.QuoteNumber.Trim();
        var codeExists = await dbContext.Quotes.AnyAsync(x => x.QuoteNumber.ToLower() == quoteNumber.ToLower(), cancellationToken);
        if (codeExists) return BadRequest("Quote number already exists.");

        var row = new Quote
        {
            QuoteNumber = quoteNumber,
            CariAccountId = request.CariAccountId,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = TrimOrNull(request.CustomerPhone),
            CustomerEmail = TrimOrNull(request.CustomerEmail),
            Status = QuoteStatus.Draft,
            QuoteDateUtc = request.QuoteDateUtc == default ? DateTime.UtcNow : request.QuoteDateUtc,
            ValidUntilUtc = request.ValidUntilUtc,
            OverallDiscountPercent = request.OverallDiscountPercent,
            TaxPercent = request.TaxPercent,
            Notes = TrimOrNull(request.Notes)
        };

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            row.Items.Add(new QuoteItem
            {
                QuoteId = row.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName.Trim(),
                Unit = item.Unit.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                SortOrder = i
            });
        }

        dbContext.Quotes.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/quotes/{row.Id}", row.Id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateQuote(Guid id, [FromBody] UpsertQuoteRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        if (row.Status is QuoteStatus.ConvertedToOrder)
            return BadRequest("Cannot edit a quote that has been converted to an order.");

        var validation = ValidateRequest(request);
        if (validation is not null) return BadRequest(validation);

        var quoteNumber = request.QuoteNumber.Trim();
        var codeExists = await dbContext.Quotes.AnyAsync(x => x.Id != id && x.QuoteNumber.ToLower() == quoteNumber.ToLower(), cancellationToken);
        if (codeExists) return BadRequest("Quote number already exists.");

        row.QuoteNumber = quoteNumber;
        row.CariAccountId = request.CariAccountId;
        row.CustomerName = request.CustomerName.Trim();
        row.CustomerPhone = TrimOrNull(request.CustomerPhone);
        row.CustomerEmail = TrimOrNull(request.CustomerEmail);
        row.QuoteDateUtc = request.QuoteDateUtc == default ? row.QuoteDateUtc : request.QuoteDateUtc;
        row.ValidUntilUtc = request.ValidUntilUtc;
        row.OverallDiscountPercent = request.OverallDiscountPercent;
        row.TaxPercent = request.TaxPercent;
        row.Notes = TrimOrNull(request.Notes);
        row.UpdatedAtUtc = DateTime.UtcNow;

        // Replace items
        foreach (var existing in row.Items.ToList())
        {
            existing.MarkAsDeleted();
        }

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            row.Items.Add(new QuoteItem
            {
                QuoteId = row.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName.Trim(),
                Unit = item.Unit.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                SortOrder = i
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateQuoteStatus(Guid id, [FromBody] UpdateQuoteStatusRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.Quotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        if (row.Status is QuoteStatus.ConvertedToOrder)
            return BadRequest("Cannot change status of a converted quote.");

        row.Status = request.Status;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/convert-to-order")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> ConvertToSalesOrder(Guid id, [FromBody] ConvertToOrderRequest request, CancellationToken cancellationToken)
    {
        var quote = await dbContext.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (quote is null) return NotFound();

        if (quote.Status is QuoteStatus.ConvertedToOrder)
            return BadRequest("Quote is already converted.");

        if (quote.Status is QuoteStatus.Rejected or QuoteStatus.Expired)
            return BadRequest("Cannot convert a rejected or expired quote.");

        if (quote.CariAccountId is null)
            return BadRequest("Quote must have a CariAccountId to convert to order.");

        var warehouseExists = await dbContext.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists) return BadRequest("Warehouse not found.");

        await using var trx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var orderNo = $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var order = new SalesOrder
        {
            OrderNo = orderNo,
            CustomerCariAccountId = quote.CariAccountId.Value,
            WarehouseId = request.WarehouseId,
            OrderDateUtc = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };

        foreach (var qi in quote.Items.Where(i => !i.IsDeleted))
        {
            if (qi.ProductId.HasValue)
            {
                var effectivePrice = qi.UnitPrice * (1 - qi.DiscountPercent / 100);
                order.Items.Add(new SalesOrderItem
                {
                    SalesOrderId = order.Id,
                    ProductId = qi.ProductId.Value,
                    Quantity = qi.Quantity,
                    UnitPrice = effectivePrice
                });
            }
        }

        dbContext.SalesOrders.Add(order);

        quote.Status = QuoteStatus.ConvertedToOrder;
        quote.ConvertedSalesOrderId = order.Id;
        quote.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);

        return Ok(order.Id);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteQuote(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        if (row.Status is QuoteStatus.ConvertedToOrder)
            return Conflict("Cannot delete a converted quote.");

        foreach (var item in row.Items) item.MarkAsDeleted();
        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /* ─── Helpers ───────────────────────────────────────────────── */

    private async Task<QuoteDto> MapToDto(Quote x, CancellationToken cancellationToken)
    {
        string? cariCode = null, cariName = null;
        if (x.CariAccountId.HasValue)
        {
            var cari = await dbContext.CariAccounts.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == x.CariAccountId.Value, cancellationToken);
            cariCode = cari?.Code;
            cariName = cari?.Name;
        }

        var items = x.Items.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder).Select(i =>
        {
            var lineTotal = i.Quantity * i.UnitPrice * (1 - i.DiscountPercent / 100);
            return new QuoteItemDto(i.Id, i.ProductId, i.ProductName, i.Unit,
                i.Quantity, i.UnitPrice, i.DiscountPercent, lineTotal, i.SortOrder);
        }).ToList();

        var subTotal = items.Sum(i => i.LineTotal);
        var discountAmount = subTotal * (x.OverallDiscountPercent / 100);
        var afterDiscount = subTotal - discountAmount;
        var taxAmount = afterDiscount * (x.TaxPercent / 100);
        var grandTotal = afterDiscount + taxAmount;

        return new QuoteDto(
            x.Id, x.QuoteNumber, x.CariAccountId, cariCode, cariName,
            x.CustomerName, x.CustomerPhone, x.CustomerEmail,
            x.Status, x.QuoteDateUtc, x.ValidUntilUtc,
            x.OverallDiscountPercent, x.TaxPercent, x.Notes,
            x.ConvertedSalesOrderId, items,
            subTotal, discountAmount, taxAmount, grandTotal,
            x.CreatedAtUtc);
    }

    private static decimal CalculateGrandTotal(Quote x)
    {
        var subTotal = x.Items.Where(i => !i.IsDeleted)
            .Sum(i => i.Quantity * i.UnitPrice * (1 - i.DiscountPercent / 100));
        var afterDiscount = subTotal - subTotal * (x.OverallDiscountPercent / 100);
        return afterDiscount + afterDiscount * (x.TaxPercent / 100);
    }

    private static string? ValidateRequest(UpsertQuoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.QuoteNumber)) return "QuoteNumber is required.";
        if (string.IsNullOrWhiteSpace(request.CustomerName)) return "CustomerName is required.";
        if (request.ValidUntilUtc == default) return "ValidUntilUtc is required.";
        if (request.Items.Count == 0) return "At least one item is required.";
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ProductName)) return "ProductName is required for all items.";
            if (item.Quantity <= 0) return "Quantity must be greater than zero.";
            if (item.UnitPrice < 0) return "UnitPrice cannot be negative.";
        }
        return null;
    }

    private static string? TrimOrNull(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
