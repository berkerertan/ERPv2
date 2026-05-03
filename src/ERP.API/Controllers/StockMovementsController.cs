using ERP.API.Common;
using ERP.API.Contracts.StockMovements;
using ERP.Application.Abstractions.Media;
using ERP.Application.Features.StockMovements.Commands.ApplyInventoryCount;
using ERP.Application.Features.StockMovements.Commands.CreateStockMovement;
using ERP.Application.Features.StockMovements.Commands.DeleteStockMovement;
using ERP.Application.Features.StockMovements.Commands.StartInventoryCountSession;
using ERP.Application.Features.StockMovements.Commands.TransferStock;
using ERP.Application.Features.StockMovements.Commands.UpdateStockMovement;
using ERP.Application.Features.StockMovements.Queries.GetCriticalStockAlerts;
using ERP.Application.Features.StockMovements.Queries.GetInventoryCountSessionById;
using ERP.Application.Features.StockMovements.Queries.GetInventoryCountSessions;
using ERP.Application.Features.StockMovements.Queries.GetStockBalances;
using ERP.Application.Features.StockMovements.Queries.GetStockMovementById;
using ERP.Application.Features.StockMovements.Queries.GetStockMovements;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/stock-movements")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class StockMovementsController(IMediator mediator, IMediaStorageService mediaStorageService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> GetAll(
        [FromQuery] string? q,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? productId,
        [FromQuery] ERP.Domain.Enums.StockMovementType? type,
        [FromQuery] ERP.Domain.Enums.StockMovementReason? reason,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sortDir = "desc",
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new GetStockMovementsQuery(q, warehouseId, productId, type, reason, fromUtc, toUtc, page, pageSize, sortDir),
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockMovementDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStockMovementByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("balances")]
    [ProducesResponseType(typeof(IReadOnlyList<StockReportItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockReportItemDto>>> GetBalances(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStockBalancesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("critical-alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<CriticalStockAlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CriticalStockAlertDto>>> GetCriticalAlerts(
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCriticalStockAlertsQuery(warehouseId), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateStockMovementRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateStockMovementCommand(
            request.WarehouseId,
            request.ProductId,
            request.Type,
            request.Quantity,
            request.UnitPrice,
            request.ReferenceNo,
            request.Reason,
            request.ReasonNote,
            request.ProofImageUrl,
            request.ProofImagePublicId);

        var id = await mediator.Send(command, cancellationToken);
        return Created($"/api/stock-movements/{id}", id);
    }

    [HttpPost("inventory-count")]
    [ProducesResponseType(typeof(ApplyInventoryCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplyInventoryCountResponse>> ApplyInventoryCount(
        [FromBody] ApplyInventoryCountRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var currentUserName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
        var result = await mediator.Send(
            new ApplyInventoryCountCommand(
                request.ClientRequestId,
                request.SessionId,
                request.WarehouseId,
                request.ReferenceNo,
                request.Notes,
                request.LocationCode,
                currentUserId,
                currentUserName,
                request.Items.Select(x => new ApplyInventoryCountItem(x.ProductId, x.CountedQuantity)).ToList()),
            cancellationToken);

        return Ok(new ApplyInventoryCountResponse(
            result.SessionId,
            result.ReferenceNo,
            result.SubmittedItems,
            result.AppliedItems,
            result.SkippedItems,
            result.TotalIncreaseQuantity,
            result.TotalDecreaseQuantity));
    }

    [HttpPost("inventory-count-sessions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> StartInventoryCountSession(
        [FromBody] StartInventoryCountSessionRequest request,
        CancellationToken cancellationToken)
    {
        var sessionId = await mediator.Send(
            new StartInventoryCountSessionCommand(
                request.WarehouseId,
                request.ReferenceNo,
                request.Notes,
                request.LocationCode,
                GetCurrentUserId(),
                User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name),
            cancellationToken);

        return Created($"/api/stock-movements/inventory-count-sessions/{sessionId}", sessionId);
    }

    [HttpGet("inventory-count-sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryCountSessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InventoryCountSessionListItemDto>>> GetInventoryCountSessions(
        [FromQuery] Guid? warehouseId,
        [FromQuery] bool includeCompleted = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(
            new GetInventoryCountSessionsQuery(warehouseId, includeCompleted, page, pageSize),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("inventory-count-sessions/{id:guid}")]
    [ProducesResponseType(typeof(InventoryCountSessionDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryCountSessionDetailDto>> GetInventoryCountSessionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetInventoryCountSessionByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferStockResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransferStockResult>> Transfer(
        [FromBody] TransferStockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new TransferStockCommand(
            request.SourceWarehouseId,
            request.DestinationWarehouseId,
            request.ProductId,
            request.Quantity,
            request.UnitPrice,
            request.ReferenceNo);

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStockMovementRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateStockMovementCommand(
            id,
            request.WarehouseId,
            request.ProductId,
            request.Type,
            request.Quantity,
            request.UnitPrice,
            request.ReferenceNo,
            request.Reason,
            request.ReasonNote,
            request.ProofImageUrl,
            request.ProofImagePublicId);

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("proof-upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(StockMovementProofUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<StockMovementProofUploadResponse>> UploadProof(
        [FromForm] StockMovementProofUploadForm form,
        CancellationToken cancellationToken = default)
    {
        if (!mediaStorageService.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Cloud media storage is not configured.");
        }

        var file = form.File;
        if (file is null || file.Length <= 0)
        {
            return BadRequest("Proof file is required.");
        }

        const long maxBytes = 15 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            return BadRequest("Proof file size cannot exceed 15 MB.");
        }

        var contentType = file.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
        var isSupportedImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var isPdf = string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        if (!isSupportedImage && !isPdf)
        {
            return BadRequest("Only image and PDF files are supported.");
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var upload = await mediaStorageService.UploadStockMovementProofAsync(
                stream,
                file.FileName,
                contentType,
                cancellationToken);

            return Ok(new StockMovementProofUploadResponse(
                upload.Url,
                upload.PublicId,
                upload.Format,
                upload.Bytes));
        }
        catch (InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Cloud media storage is temporarily unavailable.");
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Cloud media storage is temporarily unavailable.");
        }
        catch (TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Cloud media storage request timed out.");
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteStockMovementCommand(id), cancellationToken);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}




