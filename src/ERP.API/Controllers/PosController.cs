using ERP.API.Common;
using ERP.API.Contracts.Pos;
using ERP.Application.Features.Pos.Commands.CreatePosQuickSale;
using ERP.Application.Features.Pos.Queries.ScanPosProduct;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/pos")]
[RequirePolicy("TierUserOrAdmin")]
[RequireSubscriptionFeature(SubscriptionFeatures.Pos)]
public sealed class PosController(IMediator mediator) : ControllerBase
{
    [HttpGet("products/scan")]
    [ProducesResponseType(typeof(PosProductScanDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PosProductScanDto>> ScanProduct(
        [FromQuery] Guid warehouseId,
        [FromQuery] string barcode,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new ScanPosProductQuery(warehouseId, barcode), cancellationToken);
        return Ok(response);
    }

    [HttpPost("quick-sales")]
    [ProducesResponseType(typeof(PosQuickSaleResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PosQuickSaleResult>> CreateQuickSale(
        [FromBody] CreatePosQuickSaleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePosQuickSaleCommand(
            request.CustomerCariAccountId,
            request.WarehouseId,
            request.Items.Select(x => new PosQuickSaleItemInput(x.ProductId, x.Barcode, x.Quantity, x.UnitPrice)).ToList(),
            request.Note);

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }
}

