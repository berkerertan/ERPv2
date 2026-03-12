using ERP.API.Common;
using ERP.API.Contracts.StockMovements;
using ERP.Application.Features.StockMovements.Commands.CreateStockMovement;
using ERP.Application.Features.StockMovements.Commands.DeleteStockMovement;
using ERP.Application.Features.StockMovements.Commands.TransferStock;
using ERP.Application.Features.StockMovements.Commands.UpdateStockMovement;
using ERP.Application.Features.StockMovements.Queries.GetCriticalStockAlerts;
using ERP.Application.Features.StockMovements.Queries.GetStockBalances;
using ERP.Application.Features.StockMovements.Queries.GetStockMovementById;
using ERP.Application.Features.StockMovements.Queries.GetStockMovements;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/stock-movements")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class StockMovementsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> GetAll(
        [FromQuery] string? q,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productId,
        [FromQuery] ERP.Domain.Enums.StockMovementType? type,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortDir = "desc",
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new GetStockMovementsQuery(q, warehouseId, productId, type, fromUtc, toUtc, page, pageSize, sortDir),
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockMovementDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStockMovementByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("balances")]
    [ProducesResponseType(typeof(IReadOnlyList<StockReportItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockReportItemDto>>> GetBalances(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStockBalancesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("critical-alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<CriticalStockAlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CriticalStockAlertDto>>> GetCriticalAlerts(
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCriticalStockAlertsQuery(warehouseId), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateStockMovementRequest request, CancellationToken cancellationToken)
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

    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferStockResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferStockResult>> Transfer(
        [FromBody] TransferStockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TransferStockCommand(
            request.SourceWarehouseId,
            request.DestinationWarehouseId,
            request.ProductId,
            request.Quantity,
            request.UnitPrice,
            request.ReferenceNo);

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStockMovementRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateStockMovementCommand(
            id,
            request.WarehouseId,
            request.ProductId,
            request.Type,
            request.Quantity,
            request.UnitPrice,
            request.ReferenceNo);

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteStockMovementCommand(id), cancellationToken);
        return NoContent();
    }
}




