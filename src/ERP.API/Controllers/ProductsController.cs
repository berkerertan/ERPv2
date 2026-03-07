using ERP.API.Contracts.Products;
using ERP.Application.Features.Products.Commands.CreateProduct;
using ERP.Application.Features.Products.Queries.GetProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetProductsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateProductCommand(request.Code, request.Name, request.Unit, request.Category), cancellationToken);
        return Created($"/api/products/{id}", id);
    }
}
