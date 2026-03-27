using ERP.API.Common;
using ERP.API.Contracts.PriceLists;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/price-lists")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class PriceListsController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PriceListListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PriceListListDto>>> GetPriceLists(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PriceLists.AsNoTracking().Include(x => x.Items).AsQueryable();
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

        var result = rows.Select(x => new PriceListListDto(
            x.Id, x.Name, x.Description, x.IsActive,
            x.StartDate, x.EndDate, x.DiscountRate,
            x.Items.Count(i => !i.IsDeleted),
            x.CreatedAtUtc)).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PriceListDto>> GetPriceListById(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.PriceLists.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        return Ok(await MapToDto(row, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreatePriceList([FromBody] UpsertPriceListRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request);
        if (validation is not null) return BadRequest(validation);

        var row = new PriceList
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DiscountRate = request.DiscountRate
        };

        foreach (var item in request.Items)
        {
            row.Items.Add(new PriceListItem
            {
                PriceListId = row.Id,
                ProductId = item.ProductId,
                CustomPrice = item.CustomPrice
            });
        }

        dbContext.PriceLists.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/price-lists/{row.Id}", row.Id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePriceList(Guid id, [FromBody] UpsertPriceListRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.PriceLists.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        var validation = ValidateRequest(request);
        if (validation is not null) return BadRequest(validation);

        row.Name = request.Name.Trim();
        row.Description = request.Description?.Trim();
        row.IsActive = request.IsActive;
        row.StartDate = request.StartDate;
        row.EndDate = request.EndDate;
        row.DiscountRate = request.DiscountRate;
        row.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var existing in row.Items.ToList()) existing.MarkAsDeleted();
        foreach (var item in request.Items)
        {
            row.Items.Add(new PriceListItem
            {
                PriceListId = row.Id,
                ProductId = item.ProductId,
                CustomPrice = item.CustomPrice
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePriceList(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.PriceLists.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        foreach (var item in row.Items) item.MarkAsDeleted();
        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<PriceListDto> MapToDto(PriceList x, CancellationToken cancellationToken)
    {
        var productIds = x.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId).ToList();
        var products = await dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        var productMap = products.ToDictionary(p => p.Id);

        var items = x.Items.Where(i => !i.IsDeleted).Select(i =>
        {
            productMap.TryGetValue(i.ProductId, out var p);
            return new PriceListItemDto(i.Id, i.ProductId,
                p?.Name ?? "", p?.Code ?? "",
                p?.DefaultSalePrice ?? 0m, i.CustomPrice);
        }).ToList();

        return new PriceListDto(
            x.Id, x.Name, x.Description, x.IsActive,
            x.StartDate, x.EndDate, x.DiscountRate,
            items, x.CreatedAtUtc);
    }

    private static string? ValidateRequest(UpsertPriceListRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
        if (request.EndDate < request.StartDate) return "EndDate must be after StartDate.";
        if (request.DiscountRate < 0 || request.DiscountRate > 100) return "DiscountRate must be between 0 and 100.";
        return null;
    }
}
