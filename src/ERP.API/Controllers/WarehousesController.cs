using ERP.API.Contracts.Warehouses;
using ERP.Application.Features.Warehouses.Commands.CreateWarehouse;
using ERP.Application.Features.Warehouses.Commands.DeleteWarehouse;
using ERP.Application.Features.Warehouses.Commands.UpdateWarehouse;
using ERP.Application.Features.Warehouses.Queries.GetWarehouseById;
using ERP.Application.Features.Warehouses.Queries.GetWarehouses;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetWarehousesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WarehouseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetWarehouseByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateWarehouseCommand(request.BranchId, request.Code, request.Name), cancellationToken);
        return Created($"/api/warehouses/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateWarehouseCommand(id, request.BranchId, request.Code, request.Name), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteWarehouseCommand(id), cancellationToken);
        return NoContent();
    }
}
