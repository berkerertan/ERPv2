using ERP.API.Contracts.PurchaseOrders;
using ERP.Application.Features.PurchaseOrders.Commands.ApprovePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Commands.DeletePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Commands.UpdatePurchaseOrder;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById;
using ERP.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/purchase-orders")]
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
        await mediator.Send(new ApprovePurchaseOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
