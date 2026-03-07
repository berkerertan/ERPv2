using ERP.API.Contracts.Warehouses;
using ERP.Application.Features.Warehouses.Commands.CreateWarehouse;
using ERP.Application.Features.Warehouses.Queries.GetWarehouses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public sealed class WarehousesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WarehouseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetWarehousesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateWarehouseCommand(request.BranchId, request.Code, request.Name), cancellationToken);
        return Created($"/api/warehouses/{id}", id);
    }
}
