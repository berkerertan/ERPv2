using ERP.API.Common;
using ERP.API.Contracts.FinanceMovements;
using ERP.Application.Features.FinanceMovements.Commands.CreateFinanceMovement;
using ERP.Application.Features.FinanceMovements.Commands.DeleteFinanceMovement;
using ERP.Application.Features.FinanceMovements.Commands.UpdateFinanceMovement;
using ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovementById;
using ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovements;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/finance-movements")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class FinanceMovementsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FinanceMovementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FinanceMovementDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetFinanceMovementsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FinanceMovementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinanceMovementDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetFinanceMovementByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateFinanceMovementRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateFinanceMovementCommand(
            request.CariAccountId,
            request.Type,
            request.Amount,
            request.Description,
            request.ReferenceNo), cancellationToken);

        return Created($"/api/finance-movements/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFinanceMovementRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateFinanceMovementCommand(
            id,
            request.CariAccountId,
            request.Type,
            request.Amount,
            request.Description,
            request.ReferenceNo);

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteFinanceMovementCommand(id), cancellationToken);
        return NoContent();
    }
}


