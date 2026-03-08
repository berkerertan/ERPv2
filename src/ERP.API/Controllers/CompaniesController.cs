using ERP.API.Contracts.Companies;
using ERP.Application.Features.Companies.Commands.CreateCompany;
using ERP.Application.Features.Companies.Queries.GetCompanies;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Roles = AppRoles.AdminOrEmployee)]
public sealed class CompaniesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCompaniesQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateCompanyCommand(request.Code, request.Name, request.TaxNumber), cancellationToken);
        return Created($"/api/companies/{id}", id);
    }
}
