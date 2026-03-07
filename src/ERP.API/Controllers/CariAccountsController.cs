using ERP.API.Contracts.CariAccounts;
using ERP.Application.Features.CariAccounts.Commands.CreateCariAccount;
using ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/cari-accounts")]
[Authorize]
public sealed class CariAccountsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariAccountsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateCariAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCariAccountCommand(
            request.Code,
            request.Name,
            request.Type,
            request.RiskLimit,
            request.MaturityDays);

        var id = await mediator.Send(command, cancellationToken);
        return Created($"/api/cari-accounts/{id}", id);
    }
}
