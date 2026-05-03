using ERP.API.Common;
using ERP.API.Contracts.Orders;
using ERP.API.Contracts.SalesOrders;
using ERP.Application.Features.SalesOrders.Commands.ApproveSalesOrder;
using ERP.Application.Features.SalesOrders.Commands.CreateSalesOrder;
using ERP.Application.Features.SalesOrders.Commands.DeleteSalesOrder;
using ERP.Application.Features.SalesOrders.Commands.RejectSalesOrder;
using ERP.Application.Features.SalesOrders.Commands.UpdateSalesOrder;
using ERP.Application.Features.SalesOrders.Queries.GetSalesOrderById;
using ERP.Application.Features.SalesOrders.Queries.GetSalesOrders;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/sales-orders")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class SalesOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalesOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalesOrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetSalesOrdersQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesOrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetSalesOrderByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateSalesOrderCommand(
            request.OrderNo,
            request.CustomerCariAccountId,
            request.WarehouseId,
            request.Items.Select(x => new CreateSalesOrderItemInput(x.ProductId, x.Quantity, x.UnitPrice)).ToList());

        var id = await mediator.Send(command, cancellationToken);
        return Created($"/api/sales-orders/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateSalesOrderCommand(
            id,
            request.OrderNo,
            request.CustomerCariAccountId,
            request.WarehouseId,
            request.Items.Select(x => new UpdateSalesOrderItemInput(x.ProductId, x.Quantity, x.UnitPrice)).ToList());

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteSalesOrderCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ApproveSalesOrderCommand(
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
            new RejectSalesOrderCommand(
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


