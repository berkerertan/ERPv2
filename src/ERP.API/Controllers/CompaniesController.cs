using ERP.API.Contracts.Companies;
using ERP.Application.Features.Companies.Commands.CreateCompany;
using ERP.Application.Features.Companies.Commands.DeleteCompany;
using ERP.Application.Features.Companies.Commands.UpdateCompany;
using ERP.Application.Features.Companies.Queries.GetCompanies;
using ERP.Application.Features.Companies.Queries.GetCompanyById;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/companies")]
public sealed class CompaniesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCompaniesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCompanyByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateCompanyCommand(request.Code, request.Name, request.TaxNumber), cancellationToken);
        return Created($"/api/companies/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateCompanyCommand(id, request.Code, request.Name, request.TaxNumber), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCompanyCommand(id), cancellationToken);
        return NoContent();
    }
}
