using ERP.API.Contracts.Branches;
using ERP.Application.Features.Branches.Commands.CreateBranch;
using ERP.Application.Features.Branches.Queries.GetBranches;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize(Roles = AppRoles.AdminOrEmployee)]
public sealed class BranchesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BranchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetBranchesQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateBranchCommand(request.CompanyId, request.Code, request.Name), cancellationToken);
        return Created($"/api/branches/{id}", id);
    }
}
