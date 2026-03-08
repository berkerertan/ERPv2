using ERP.API.Contracts.FinanceMovements;
using ERP.Application.Features.FinanceMovements.Commands.CreateFinanceMovement;
using ERP.Application.Features.FinanceMovements.Queries.GetFinanceMovements;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/finance-movements")]
[Authorize(Roles = AppRoles.AdminOrEmployee)]
public sealed class FinanceMovementsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FinanceMovementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FinanceMovementDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetFinanceMovementsQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = AppRoles.Admin)]
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
}
