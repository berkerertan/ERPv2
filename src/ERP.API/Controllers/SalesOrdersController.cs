using ERP.API.Contracts.SalesOrders;
using ERP.Application.Features.SalesOrders.Commands.ApproveSalesOrder;
using ERP.Application.Features.SalesOrders.Commands.CreateSalesOrder;
using ERP.Application.Features.SalesOrders.Queries.GetSalesOrders;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/sales-orders")]
[Authorize(Roles = AppRoles.AdminOrEmployee)]
public sealed class SalesOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalesOrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalesOrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetSalesOrdersQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = AppRoles.Admin)]
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new ApproveSalesOrderCommand(id), cancellationToken);
        return NoContent();
    }
}
