using ERP.API.Common;
using ERP.API.Contracts.Waybills;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/waybills")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class WaybillsController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WaybillListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WaybillListDto>>> GetWaybills(
        [FromQuery] string? q,
        [FromQuery] WaybillStatus? status,
        [FromQuery] WaybillType? type,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Waybills.AsNoTracking().Include(x => x.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x => x.WaybillNo.ToLower().Contains(term));
        }
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (type.HasValue) query = query.Where(x => x.Type == type.Value);

        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

        var cariIds = rows.Select(x => x.CariAccountId).Distinct().ToList();
        var whIds = rows.Select(x => x.WarehouseId).Distinct().ToList();

        var cariMap = await dbContext.CariAccounts.AsNoTracking()
            .Where(x => cariIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var whMap = await dbContext.Warehouses.AsNoTracking()
            .Where(x => whIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var result = rows.Select(x => new WaybillListDto(
            x.Id, x.WaybillNo, x.Type,
            x.CariAccountId, cariMap.GetValueOrDefault(x.CariAccountId, ""),
            x.WarehouseId, whMap.GetValueOrDefault(x.WarehouseId, ""),
            x.Status, x.ShipDateUtc,
            x.Items.Count(i => !i.IsDeleted),
            x.CreatedAtUtc)).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WaybillDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WaybillDto>> GetWaybillById(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Waybills.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        return Ok(await MapToDto(row, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateWaybill([FromBody] CreateWaybillRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0) return BadRequest("At least one item is required.");

        var waybillNo = $"WB-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var row = new Waybill
        {
            WaybillNo = waybillNo,
            Type = request.Type,
            CariAccountId = request.CariAccountId,
            WarehouseId = request.WarehouseId,
            Status = WaybillStatus.Draft,
            DeliveryAddress = request.DeliveryAddress?.Trim(),
            Notes = request.Notes?.Trim()
        };

        foreach (var item in request.Items)
        {
            row.Items.Add(new WaybillItem
            {
                WaybillId = row.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        dbContext.Waybills.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/waybills/{row.Id}", row.Id);
    }

    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Ship(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Waybills.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();
        if (row.Status != WaybillStatus.Draft) return BadRequest("Only draft waybills can be shipped.");

        row.Status = WaybillStatus.Shipped;
        row.ShipDateUtc = DateTime.UtcNow;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Waybills.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();
        if (row.Status != WaybillStatus.Shipped) return BadRequest("Only shipped waybills can be delivered.");

        row.Status = WaybillStatus.Delivered;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Waybills.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();
        if (row.Status == WaybillStatus.Delivered) return BadRequest("Delivered waybills cannot be cancelled.");

        row.Status = WaybillStatus.Cancelled;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteWaybill(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Waybills.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();
        if (row.Status == WaybillStatus.Delivered) return Conflict("Delivered waybills cannot be deleted.");

        foreach (var item in row.Items) item.MarkAsDeleted();
        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<WaybillDto> MapToDto(Waybill x, CancellationToken cancellationToken)
    {
        var cari = await dbContext.CariAccounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == x.CariAccountId, cancellationToken);
        var wh = await dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == x.WarehouseId, cancellationToken);

        var productIds = x.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId).ToList();
        var productMap = await dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var items = x.Items.Where(i => !i.IsDeleted).Select(i =>
            new WaybillItemDto(i.Id, i.ProductId, productMap.GetValueOrDefault(i.ProductId, ""), i.Quantity, i.UnitPrice)
        ).ToList();

        return new WaybillDto(
            x.Id, x.WaybillNo, x.Type,
            x.CariAccountId, cari?.Name ?? "",
            x.WarehouseId, wh?.Name ?? "",
            x.Status, x.ShipDateUtc,
            x.DeliveryAddress, x.Notes,
            items, x.CreatedAtUtc);
    }
}
