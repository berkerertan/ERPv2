using ERP.API.Common;
using ERP.API.Contracts.Returns;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/returns")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class ReturnsController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReturnListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReturnListDto>>> GetReturns(
        [FromQuery] string? q,
        [FromQuery] ReturnStatus? status,
        [FromQuery] ReturnType? type,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Returns.AsNoTracking().Include(x => x.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x => x.ReturnNo.ToLower().Contains(term));
        }
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (type.HasValue) query = query.Where(x => x.Type == type.Value);

        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

        var cariIds = rows.Select(x => x.CariAccountId).Distinct().ToList();
        var cariMap = await dbContext.CariAccounts.AsNoTracking()
            .Where(x => cariIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var result = rows.Select(x =>
        {
            var items = x.Items.Where(i => !i.IsDeleted).ToList();
            var total = items.Sum(i => i.Quantity * i.UnitPrice);
            return new ReturnListDto(
                x.Id, x.ReturnNo, x.Type,
                x.CariAccountId, cariMap.GetValueOrDefault(x.CariAccountId, ""),
                x.Status, x.ReturnDateUtc, items.Count, total, x.CreatedAtUtc);
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReturnDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReturnDto>> GetReturnById(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Returns.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        return Ok(await MapToDto(row, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateReturn([FromBody] CreateReturnRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0) return BadRequest("At least one item is required.");

        var returnNo = $"RT-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var row = new Return
        {
            ReturnNo = returnNo,
            Type = request.Type,
            CariAccountId = request.CariAccountId,
            WarehouseId = request.WarehouseId,
            Status = ReturnStatus.Pending,
            ReturnDateUtc = DateTime.UtcNow,
            Reason = request.Reason?.Trim()
        };

        foreach (var item in request.Items)
        {
            row.Items.Add(new ReturnItem
            {
                ReturnId = row.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        dbContext.Returns.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/returns/{row.Id}", row.Id);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Returns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();
        if (row.Status != ReturnStatus.Pending) return BadRequest("Only pending returns can be approved.");

        row.Status = ReturnStatus.Approved;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Returns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();
        if (row.Status != ReturnStatus.Pending) return BadRequest("Only pending returns can be rejected.");

        row.Status = ReturnStatus.Rejected;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteReturn(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.Returns.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();
        if (row.Status == ReturnStatus.Approved) return Conflict("Approved returns cannot be deleted.");

        foreach (var item in row.Items) item.MarkAsDeleted();
        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<ReturnDto> MapToDto(Return x, CancellationToken cancellationToken)
    {
        var cari = await dbContext.CariAccounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == x.CariAccountId, cancellationToken);
        var wh = await dbContext.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == x.WarehouseId, cancellationToken);

        var productIds = x.Items.Where(i => !i.IsDeleted).Select(i => i.ProductId).ToList();
        var productMap = await dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var items = x.Items.Where(i => !i.IsDeleted).Select(i =>
            new ReturnItemDto(i.Id, i.ProductId, productMap.GetValueOrDefault(i.ProductId, ""),
                i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice)
        ).ToList();

        var total = items.Sum(i => i.LineTotal);

        return new ReturnDto(
            x.Id, x.ReturnNo, x.Type,
            x.CariAccountId, cari?.Name ?? "",
            x.WarehouseId, wh?.Name ?? "",
            x.Status, x.ReturnDateUtc, x.Reason,
            items, total, x.CreatedAtUtc);
    }
}
