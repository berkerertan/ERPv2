using ERP.API.Common;
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
using ERP.Application.Features.CariAccounts.Queries.GetCariAccountSuggestions;
using ERP.Application.Features.CariAccounts.Queries.GetCariDebtItemById;
using ERP.Application.Features.CariAccounts.Queries.GetCariDebtItems;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/cari-accounts")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class CariAccountsController(IMediator mediator, ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountDto>>> GetAll(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc",
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetCariAccountsQuery(q, null, page, pageSize, sortBy, sortDir), cancellationToken);
        return Ok(response);
    }

    [HttpGet("suppliers")]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountDto>>> GetSuppliers(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc",
        CancellationToken cancellationToken = default)
    {
        var suppliers = await mediator.Send(new GetCariAccountsQuery(q, CariType.Supplier, page, pageSize, sortBy, sortDir), cancellationToken);
        return Ok(suppliers);
    }

    [HttpGet("buyers")]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountDto>>> GetBuyers(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc",
        CancellationToken cancellationToken = default)
    {
        var buyers = await mediator.Send(new GetCariAccountsQuery(q, CariType.BuyerBch, page, pageSize, sortBy, sortDir), cancellationToken);
        return Ok(buyers);
    }


    [HttpGet("suggest")]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountSuggestionDto>>> Suggest(
        [FromQuery] string? q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetCariAccountSuggestionsQuery(q, null, limit), cancellationToken);
        return Ok(response);
    }

    [HttpGet("buyers/suggest")]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountSuggestionDto>>> SuggestBuyers(
        [FromQuery] string? q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetCariAccountSuggestionsQuery(q, CariType.BuyerBch, limit), cancellationToken);
        return Ok(response);
    }

    [HttpGet("suppliers/suggest")]
    [ProducesResponseType(typeof(IReadOnlyList<CariAccountSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAccountSuggestionDto>>> SuggestSuppliers(
        [FromQuery] string? q,
        [FromQuery] int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetCariAccountSuggestionsQuery(q, CariType.Supplier, limit), cancellationToken);
        return Ok(response);
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

    [HttpPost("buyers/import-excel")]
    [RequireSubscriptionFeature(SubscriptionFeatures.ExcelImport)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BuyerDebtItemsBatchImportResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BuyerDebtItemsBatchImportResult>> ImportBuyerDebtItems(
        [FromForm] ImportBuyerDebtItemsBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Files is null || request.Files.Count == 0)
        {
            return BadRequest("At least one Excel file is required.");
        }

        var mapping = new CariDebtItemImportColumnMapping(
            request.TransactionDateColumn,
            request.MaterialDescriptionColumn,
            request.QuantityColumn,
            request.ListPriceColumn,
            request.SalePriceColumn,
            request.TotalAmountColumn,
            request.PaymentColumn,
            request.RemainingBalanceColumn);

        var results = new List<BuyerDebtItemsBatchImportFileResult>();
        var replacedAccounts = new HashSet<Guid>();
        var createdCariCount = 0;
        var totalRows = 0;
        var totalCreatedCount = 0;
        var totalFailedCount = 0;

        foreach (var file in request.Files)
        {
            var safeFileName = file?.FileName ?? string.Empty;
            if (file is null || file.Length == 0)
            {
                results.Add(new BuyerDebtItemsBatchImportFileResult(
                    safeFileName,
                    null,
                    string.Empty,
                    false,
                    0,
                    0,
                    1,
                    ["File is empty or invalid."]));
                totalFailedCount++;
                continue;
            }

            var buyerName = ExtractBuyerNameFromFileName(safeFileName);
            if (string.IsNullOrWhiteSpace(buyerName))
            {
                results.Add(new BuyerDebtItemsBatchImportFileResult(
                    safeFileName,
                    null,
                    string.Empty,
                    false,
                    0,
                    0,
                    1,
                    ["Buyer name could not be resolved from file name."]));
                totalFailedCount++;
                continue;
            }

            var normalizedName = buyerName.Trim().ToLowerInvariant();
            var account = await dbContext.CariAccounts.FirstOrDefaultAsync(
                x => x.Name.ToLower() == normalizedName,
                cancellationToken);

            var cariCreated = false;
            if (account is null)
            {
                account = new CariAccount
                {
                    Code = await GenerateUniqueBuyerCodeAsync(buyerName, cancellationToken),
                    Name = buyerName,
                    Type = CariType.BuyerBch,
                    RiskLimit = 0m,
                    MaturityDays = 0,
                    CurrentBalance = 0m
                };

                dbContext.CariAccounts.Add(account);
                await dbContext.SaveChangesAsync(cancellationToken);
                cariCreated = true;
                createdCariCount++;
            }
            else if (account.Type == CariType.Supplier)
            {
                account.Type = CariType.Both;
                account.UpdatedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);

            try
            {
                var replaceExisting = request.ReplaceExisting && replacedAccounts.Add(account.Id);
                var response = await mediator.Send(
                    new ImportCariDebtItemsCommand(account.Id, memoryStream.ToArray(), replaceExisting, mapping),
                    cancellationToken);

                totalRows += response.TotalRows;
                totalCreatedCount += response.CreatedCount;
                totalFailedCount += response.FailedCount;

                results.Add(new BuyerDebtItemsBatchImportFileResult(
                    safeFileName,
                    account.Id,
                    account.Name,
                    cariCreated,
                    response.TotalRows,
                    response.CreatedCount,
                    response.FailedCount,
                    response.Errors));
            }
            catch (Exception ex)
            {
                totalFailedCount++;
                results.Add(new BuyerDebtItemsBatchImportFileResult(
                    safeFileName,
                    account.Id,
                    account.Name,
                    cariCreated,
                    0,
                    0,
                    1,
                    [ex.Message]));
            }
        }

        return Ok(new BuyerDebtItemsBatchImportResult(
            request.Files.Count,
            results.Count,
            createdCariCount,
            totalRows,
            totalCreatedCount,
            totalFailedCount,
            results));
    }

    [HttpPost("{cariAccountId:guid}/debt-items/import-excel")]
    [RequireSubscriptionFeature(SubscriptionFeatures.ExcelImport)]
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

    private static string ExtractBuyerNameFromFileName(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return string.Empty;
        }

        var normalized = baseName
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        normalized = CollapseWhitespace(normalized);
        if (normalized.Length > 150)
        {
            normalized = normalized[..150].Trim();
        }

        return normalized;
    }

    private async Task<string> GenerateUniqueBuyerCodeAsync(string buyerName, CancellationToken cancellationToken)
    {
        var token = NormalizeCodeToken(buyerName);
        if (string.IsNullOrWhiteSpace(token))
        {
            token = "BCH";
        }

        var candidate = $"BCH-{token}";
        if (candidate.Length > 25)
        {
            candidate = candidate[..25];
        }

        var seed = candidate;
        var sequence = 1;

        while (await dbContext.CariAccounts.AnyAsync(x => x.Code == candidate, cancellationToken))
        {
            sequence++;
            var suffix = $"-{sequence}";
            var prefixLength = Math.Max(1, 25 - suffix.Length);
            candidate = string.Concat(seed[..Math.Min(seed.Length, prefixLength)], suffix);
        }

        return candidate;
    }

    private static string NormalizeCodeToken(string value)
    {
        var upper = value
            .Trim()
            .ToUpperInvariant()
            .Replace("İ", "I")
            .Replace("I", "I")
            .Replace("Ç", "C")
            .Replace("Ğ", "G")
            .Replace("Ö", "O")
            .Replace("Ş", "S")
            .Replace("Ü", "U");

        var sb = new StringBuilder(upper.Length);
        foreach (var ch in upper)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static string CollapseWhitespace(string value)
    {
        var sb = new StringBuilder(value.Length);
        var previousWhitespace = false;

        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (previousWhitespace)
                {
                    continue;
                }

                sb.Append(' ');
                previousWhitespace = true;
                continue;
            }

            sb.Append(ch);
            previousWhitespace = false;
        }

        return sb.ToString().Trim();
    }
}



