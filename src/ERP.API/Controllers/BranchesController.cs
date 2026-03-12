using ERP.API.Common;
using ERP.API.Contracts.Branches;
using ERP.Application.Features.Branches.Commands.CreateBranch;
using ERP.Application.Features.Branches.Commands.DeleteBranch;
using ERP.Application.Features.Branches.Commands.UpdateBranch;
using ERP.Application.Features.Branches.Queries.GetBranchById;
using ERP.Application.Features.Branches.Queries.GetBranches;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/branches")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class BranchesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BranchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetBranchesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BranchDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BranchDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetBranchByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateBranchCommand(request.CompanyId, request.Code, request.Name), cancellationToken);
        return Created($"/api/branches/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateBranchCommand(id, request.CompanyId, request.Code, request.Name), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteBranchCommand(id), cancellationToken);
        return NoContent();
    }
}


