using ERP.API.Contracts.Products;
using ERP.Application.Features.Products.Commands.CreateProduct;
using ERP.Application.Features.Products.Commands.DeleteProduct;
using ERP.Application.Features.Products.Commands.UpdateProduct;
using ERP.Application.Features.Products.Queries.GetProductById;
using ERP.Application.Features.Products.Queries.GetProducts;
using ERP.Application.Features.Products.Queries.GetProductSuggestions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc",
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetProductsQuery(q, page, pageSize, sortBy, sortDir), cancellationToken);
        return Ok(response);
    }


    [HttpGet("suggest")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductSuggestionDto>>> Suggest(
        [FromQuery] string? q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetProductSuggestionsQuery(q, limit), cancellationToken);
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
        var id = await mediator.Send(new CreateProductCommand(
            request.Code,
            request.Name,
            request.Unit,
            request.Category,
            request.BarcodeEan13,
            request.QrCode,
            request.DefaultSalePrice,
            request.CriticalStockLevel), cancellationToken);

        return Created($"/api/products/{id}", id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new UpdateProductCommand(
            id,
            request.Code,
            request.Name,
            request.Unit,
            request.Category,
            request.BarcodeEan13,
            request.QrCode,
            request.DefaultSalePrice,
            request.CriticalStockLevel), cancellationToken);

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

