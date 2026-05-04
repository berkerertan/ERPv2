using ERP.API.Common;
using ERP.API.Contracts.Orders;
using ERP.API.Contracts.PurchaseOrders;
using ERP.Application.Features.PurchaseOrders.Commands.ApprovePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Commands.DeletePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Commands.RejectPurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Commands.UpdatePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistory;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendationHistoryById;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseRecommendations;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class PurchaseOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetPurchaseOrdersQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetPurchaseOrderByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(PurchaseRecommendationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PurchaseRecommendationDto>> GetRecommendations(
        [FromQuery] Guid warehouseId,
        [FromQuery] Guid? supplierCariAccountId,
        [FromQuery] int analysisDays = 30,
        [FromQuery] int coverageDays = 21,
        [FromQuery] int maxItems = 30,
        [FromQuery] bool criticalOnly = false,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new GetPurchaseRecommendationsQuery(
                warehouseId,
                supplierCariAccountId,
                analysisDays,
                coverageDays,
                maxItems,
                criticalOnly,
                User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("recommendations/history")]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseRecommendationHistoryListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PurchaseRecommendationHistoryListItemDto>>> GetRecommendationHistory(
        [FromQuery] int take = 12,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetPurchaseRecommendationHistoryQuery(take), cancellationToken);
        return Ok(response);
    }

    [HttpGet("recommendations/history/{snapshotId:guid}")]
    [ProducesResponseType(typeof(PurchaseRecommendationHistoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseRecommendationHistoryDetailDto>> GetRecommendationHistoryById(
        Guid snapshotId,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetPurchaseRecommendationHistoryByIdQuery(snapshotId), cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePurchaseOrderCommand(
            request.OrderNo,
            request.SupplierCariAccountId,
            request.WarehouseId,
            request.Items.Select(x => new CreatePurchaseOrderItemInput(x.ProductId, x.Quantity, x.UnitPrice)).ToList());

        var id = await mediator.Send(command, cancellationToken);
        return Created($"/api/purchase-orders/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePurchaseOrderCommand(
            id,
            request.OrderNo,
            request.SupplierCariAccountId,
            request.WarehouseId,
            request.Items.Select(x => new UpdatePurchaseOrderItemInput(x.ProductId, x.Quantity, x.UnitPrice)).ToList());

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeletePurchaseOrderCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ApprovePurchaseOrderCommand(
                id,
                GetCurrentUserId(),
                User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectOrderRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new RejectPurchaseOrderCommand(
                id,
                request.Reason,
                GetCurrentUserId(),
                User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name),
            cancellationToken);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}


