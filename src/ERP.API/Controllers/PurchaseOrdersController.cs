using ERP.API.Contracts.PurchaseOrders;
using ERP.Application.Features.PurchaseOrders.Commands.ApprovePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize(Roles = AppRoles.AdminOrEmployee)]
public sealed class PurchaseOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetPurchaseOrdersQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = AppRoles.Admin)]
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new ApprovePurchaseOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
