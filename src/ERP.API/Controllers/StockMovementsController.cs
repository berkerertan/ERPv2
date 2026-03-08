using ERP.API.Contracts.StockMovements;
using ERP.Application.Features.StockMovements.Commands.CreateStockMovement;
using ERP.Application.Features.StockMovements.Queries.GetStockBalances;
using ERP.Application.Features.StockMovements.Queries.GetStockMovements;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/stock-movements")]
[Authorize(Roles = AppRoles.AdminOrEmployee)]
public sealed class StockMovementsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStockMovementsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("balances")]
    [ProducesResponseType(typeof(IReadOnlyList<StockBalanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockBalanceDto>>> GetBalances(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStockBalancesQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateStockMovementRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStockMovementCommand(
            request.WarehouseId,
            request.ProductId,
            request.Type,
            request.Quantity,
            request.UnitPrice,
            request.ReferenceNo);

        var id = await mediator.Send(command, cancellationToken);
        return Created($"/api/stock-movements/{id}", id);
    }
}
