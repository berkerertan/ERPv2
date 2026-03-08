using ERP.API.Contracts.CariAccounts;
using ERP.Application.Common.Models;
using ERP.Application.Features.CariAccounts.Commands.CreateCariAccount;
using ERP.Application.Features.CariAccounts.Commands.CreateCariDebtItem;
using ERP.Application.Features.CariAccounts.Commands.DeleteCariAccount;
using ERP.Application.Features.CariAccounts.Commands.DeleteCariDebtItem;
using ERP.Application.Features.CariAccounts.Commands.ImportCariDebtItems;
using ERP.Application.Features.CariAccounts.Commands.UpdateCariAccount;
using ERP.Application.Features.CariAccounts.Commands.UpdateCariDebtItem;
using ERP.Application.Features.CariAccounts.Queries.GetCariAccountById;
using ERP.Application.Features.CariAccounts.Queries.GetCariAccounts;
using ERP.Application.Features.CariAccounts.Queries.GetCariDebtItemById;
using ERP.Application.Features.CariAccounts.Queries.GetCariDebtItems;
using ERP.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/cari-accounts")]
public sealed class CariAccountsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountDto>>> GetAll(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariAccountsQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("suppliers")]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountDto>>> GetSuppliers(CancellationToken cancellationToken)
    {
        var accounts = await mediator.Send(new GetCariAccountsQuery(), cancellationToken);
        var suppliers = accounts
            .Where(x => x.Type is CariType.Supplier or CariType.Both)
            .ToList();

        return Ok(suppliers);
    }

    [HttpGet("buyers")]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountDto>>> GetBuyers(CancellationToken cancellationToken)
    {
        var accounts = await mediator.Send(new GetCariAccountsQuery(), cancellationToken);
        var buyers = accounts
            .Where(x => x.Type is CariType.BuyerBch or CariType.Both)
            .ToList();

        return Ok(buyers);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CariAccountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CariAccountDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariAccountByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(CariAccountDetailsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CariAccountDetailsResponse>> GetDetails(Guid id, CancellationToken cancellationToken)
    {
        var account = await mediator.Send(new GetCariAccountByIdQuery(id), cancellationToken);
        var items = await mediator.Send(new GetCariDebtItemsQuery(id), cancellationToken);

        return Ok(new CariAccountDetailsResponse(account, items));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCariAccountRequest request, CancellationToken cancellationToken)
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

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCariAccountRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCariAccountCommand(
            id,
            request.Code,
            request.Name,
            request.Type,
            request.RiskLimit,
            request.MaturityDays);

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCariAccountCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{cariAccountId:guid}/debt-items")]
    [ProducesResponseType(typeof(IReadOnlyList<CariDebtItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariDebtItemDto>>> GetDebtItems(Guid cariAccountId, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariDebtItemsQuery(cariAccountId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{cariAccountId:guid}/debt-items/{debtItemId:guid}")]
    [ProducesResponseType(typeof(CariDebtItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CariDebtItemDto>> GetDebtItemById(Guid cariAccountId, Guid debtItemId, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariDebtItemByIdQuery(cariAccountId, debtItemId), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{cariAccountId:guid}/debt-items")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateDebtItem(Guid cariAccountId, [FromBody] CreateCariDebtItemRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCariDebtItemCommand(
            cariAccountId,
            request.TransactionDate,
            request.MaterialDescription,
            request.Quantity,
            request.ListPrice,
            request.SalePrice,
            request.TotalAmount,
            request.Payment,
            request.RemainingBalance);

        var id = await mediator.Send(command, cancellationToken);
        return Created($"/api/cari-accounts/{cariAccountId}/debt-items/{id}", id);
    }

    [HttpPut("{cariAccountId:guid}/debt-items/{debtItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDebtItem(Guid cariAccountId, Guid debtItemId, [FromBody] UpdateCariDebtItemRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCariDebtItemCommand(
            cariAccountId,
            debtItemId,
            request.TransactionDate,
            request.MaterialDescription,
            request.Quantity,
            request.ListPrice,
            request.SalePrice,
            request.TotalAmount,
            request.Payment,
            request.RemainingBalance);

        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{cariAccountId:guid}/debt-items/{debtItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDebtItem(Guid cariAccountId, Guid debtItemId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCariDebtItemCommand(cariAccountId, debtItemId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{cariAccountId:guid}/debt-items/import-excel")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CariDebtItemImportResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CariDebtItemImportResult>> ImportDebtItems(
        Guid cariAccountId,
        [FromForm] ImportCariDebtItemsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("Excel file is required.");
        }

        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var mapping = new CariDebtItemImportColumnMapping(
            request.TransactionDateColumn,
            request.MaterialDescriptionColumn,
            request.QuantityColumn,
            request.ListPriceColumn,
            request.SalePriceColumn,
            request.TotalAmountColumn,
            request.PaymentColumn,
            request.RemainingBalanceColumn);

        var command = new ImportCariDebtItemsCommand(cariAccountId, memoryStream.ToArray(), request.ReplaceExisting, mapping);
        var response = await mediator.Send(command, cancellationToken);

        return Ok(response);
    }
}
