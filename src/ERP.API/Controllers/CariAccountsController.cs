using ERP.API.Contracts.CariAccounts;
using ERP.Application.Common.Models;
using ERP.Application.Features.CariAccounts.Commands.CreateCariAccount;
using ERP.Application.Features.CariAccounts.Commands.ImportCariAccounts;
using ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/cari-accounts")]
[Authorize(Roles = AppRoles.AdminOrEmployee)]
public sealed class CariAccountsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariAccountsQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = AppRoles.Admin)]
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

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("import-excel")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CariAccountImportResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CariAccountImportResult>> ImportExcel(
        [FromForm] ImportCariAccountsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("Excel file is required.");
        }

        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var command = new ImportCariAccountsCommand(memoryStream.ToArray(), request.UpsertExisting);
        var response = await mediator.Send(command, cancellationToken);

        return Ok(response);
    }
}
