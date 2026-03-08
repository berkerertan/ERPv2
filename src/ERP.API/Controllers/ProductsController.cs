using ERP.API.Contracts.Products;
using ERP.Application.Features.Products.Commands.CreateProduct;
using ERP.Application.Features.Products.Commands.DeleteProduct;
using ERP.Application.Features.Products.Commands.UpdateProduct;
using ERP.Application.Features.Products.Queries.GetProductById;
using ERP.Application.Features.Products.Queries.GetProducts;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetProductsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateProductCommand(request.Code, request.Name, request.Unit, request.Category, request.BarcodeEan13, request.QrCode, request.DefaultSalePrice, request.CriticalStockLevel), cancellationToken);
        return Created($"/api/products/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateProductCommand(id, request.Code, request.Name, request.Unit, request.Category, request.BarcodeEan13, request.QrCode, request.DefaultSalePrice, request.CriticalStockLevel), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}


