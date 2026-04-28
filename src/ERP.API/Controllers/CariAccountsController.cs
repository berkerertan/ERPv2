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
using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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

    [HttpGet("buyers/risk-summary")]
    [ProducesResponseType(typeof(BuyerRiskSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BuyerRiskSummaryResponse>> GetBuyerRiskSummary(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var buyerAccounts = await dbContext.CariAccounts
            .AsNoTracking()
            .Where(x => x.Type == CariType.BuyerBch || x.Type == CariType.Both)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.CurrentBalance,
                x.RiskLimit,
                x.MaturityDays
            })
            .ToListAsync(cancellationToken);

        var buyerIds = buyerAccounts.Select(x => x.Id).ToList();
        var debtItems = buyerIds.Count == 0
            ? []
            : await dbContext.CariDebtItems
                .AsNoTracking()
                .Where(x => buyerIds.Contains(x.CariAccountId) && x.RemainingBalance > 0)
                .Select(x => new
                {
                    x.CariAccountId,
                    x.TransactionDate,
                    x.RemainingBalance
                })
                .ToListAsync(cancellationToken);

        var items = buyerAccounts
            .Select(account =>
            {
                var thresholdDate = today.AddDays(-(account.MaturityDays > 0 ? account.MaturityDays : 0));
                var overdueItems = debtItems
                    .Where(x => x.CariAccountId == account.Id && x.TransactionDate.Date <= thresholdDate)
                    .ToList();

                var overdueAmount = overdueItems.Sum(x => x.RemainingBalance);
                var oldestDate = overdueItems.Count > 0
                    ? overdueItems.Min(x => x.TransactionDate).Date
                    : (DateTime?)null;
                var maxOverdueDays = oldestDate.HasValue
                    ? Math.Max(0, (today - oldestDate.Value).Days)
                    : 0;
                var riskUsageRate = account.RiskLimit > 0
                    ? decimal.Round(account.CurrentBalance / account.RiskLimit, 2, MidpointRounding.AwayFromZero)
                    : account.CurrentBalance > 0 ? 1m : 0m;

                var severity = overdueAmount > 0 && (riskUsageRate >= 1m || maxOverdueDays >= 30)
                    ? "critical"
                    : overdueAmount > 0 || riskUsageRate >= 0.8m
                        ? "warning"
                        : "stable";

                return new BuyerRiskSummaryItemDto(
                    account.Id,
                    account.Code,
                    account.Name,
                    account.CurrentBalance,
                    account.RiskLimit,
                    account.MaturityDays,
                    decimal.Round(overdueAmount, 2, MidpointRounding.AwayFromZero),
                    maxOverdueDays,
                    riskUsageRate,
                    severity);
            })
            .OrderByDescending(x => x.Severity == "critical")
            .ThenByDescending(x => x.OverdueAmount)
            .ThenByDescending(x => x.RiskUsageRate)
            .Take(Math.Clamp(limit, 1, 100))
            .ToList();

        var response = new BuyerRiskSummaryResponse(
            buyerAccounts.Count,
            items.Count(x => x.Severity != "stable"),
            items.Count(x => x.Severity == "critical"),
            buyerAccounts.Sum(x => x.CurrentBalance),
            items.Sum(x => x.OverdueAmount),
            items);

        return Ok(response);
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
            request.MaturityDays,
            request.Phone);

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
            request.MaturityDays,
            request.Phone);

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

    [HttpGet("{cariAccountId:guid}/debt-items/export-excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDebtItemsExcel(
        Guid cariAccountId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.CariAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cariAccountId, cancellationToken);

        if (account is null)
        {
            return NotFound("Cari account not found.");
        }

        var items = await BuildDebtItemExportQuery(cariAccountId, fromDate, toDate)
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var fileBytes = BuildDebtItemsExcel(account, items);
        var fileName = BuildSafeFileName($"cari-{account.Code}-{account.Name}-debt-items-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("{cariAccountId:guid}/debt-items/export-pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDebtItemsPdf(
        Guid cariAccountId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.CariAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == cariAccountId, cancellationToken);

        if (account is null)
        {
            return NotFound("Cari account not found.");
        }

        var items = await BuildDebtItemExportQuery(cariAccountId, fromDate, toDate)
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var fileBytes = BuildDebtItemsPdf(account, items);
        var fileName = BuildSafeFileName($"cari-{account.Code}-{account.Name}-debt-items-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");

        return File(fileBytes, "application/pdf", fileName);
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

    private IQueryable<CariDebtItem> BuildDebtItemExportQuery(Guid cariAccountId, DateTime? fromDate, DateTime? toDate)
    {
        var query = dbContext.CariDebtItems
            .AsNoTracking()
            .Where(x => x.CariAccountId == cariAccountId);

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(x => x.TransactionDate >= from);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.TransactionDate < toExclusive);
        }

        return query;
    }

    private static byte[] BuildDebtItemsExcel(CariAccount account, IReadOnlyList<CariDebtItem> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("DebtItems");

        worksheet.Cell(1, 1).Value = "TransactionDate";
        worksheet.Cell(1, 2).Value = "MaterialDescription";
        worksheet.Cell(1, 3).Value = "Quantity";
        worksheet.Cell(1, 4).Value = "ListPrice";
        worksheet.Cell(1, 5).Value = "SalePrice";
        worksheet.Cell(1, 6).Value = "TotalAmount";
        worksheet.Cell(1, 7).Value = "Payment";
        worksheet.Cell(1, 8).Value = "RemainingBalance";

        var headerRange = worksheet.Range(1, 1, 1, 8);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        var row = 2;
        foreach (var item in items)
        {
            worksheet.Cell(row, 1).Value = item.TransactionDate.Date;
            worksheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-MM-dd";
            worksheet.Cell(row, 2).Value = item.MaterialDescription;
            worksheet.Cell(row, 3).Value = item.Quantity;
            worksheet.Cell(row, 4).Value = item.ListPrice;
            worksheet.Cell(row, 5).Value = item.SalePrice;
            worksheet.Cell(row, 6).Value = item.TotalAmount;
            worksheet.Cell(row, 7).Value = item.Payment;
            worksheet.Cell(row, 8).Value = item.RemainingBalance;

            worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.###";
            worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        worksheet.Columns(1, 8).AdjustToContents();
        worksheet.Cell(1, 10).Value = $"CariCode: {account.Code}";
        worksheet.Cell(2, 10).Value = $"CariName: {account.Name}";
        worksheet.Cell(3, 10).Value = $"ExportedAtUtc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildDebtItemsPdf(CariAccount account, IReadOnlyList<CariDebtItem> items)
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var printableLines = new List<string>
        {
            $"Cari Debt List - {ToAscii(account.Name)} ({ToAscii(account.Code)})",
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            "Date       Material                                   Qty      List       Sale       Total      Paid       Remain",
            "---------------------------------------------------------------------------------------------------------------"
        };

        if (items.Count == 0)
        {
            printableLines.Add("No debt items found for selected filters.");
        }
        else
        {
            foreach (var item in items)
            {
                printableLines.Add(
                    $"{item.TransactionDate:yyyy-MM-dd} " +
                    $"{TrimForPdf(ToAscii(item.MaterialDescription), 40),-40} " +
                    $"{item.Quantity.ToString("N3", tr),8} " +
                    $"{item.ListPrice.ToString("N2", tr),10} " +
                    $"{item.SalePrice.ToString("N2", tr),10} " +
                    $"{item.TotalAmount.ToString("N2", tr),10} " +
                    $"{item.Payment.ToString("N2", tr),10} " +
                    $"{item.RemainingBalance.ToString("N2", tr),10}");
            }
        }

        return BuildPlainTextPdf(printableLines, maxLinesPerPage: 46);
    }

    private static string TrimForPdf(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var value = input.Trim();
        return value.Length <= maxLength ? value : $"{value[..Math.Max(0, maxLength - 3)]}...";
    }

    private static byte[] BuildPlainTextPdf(IReadOnlyList<string> lines, int maxLinesPerPage)
    {
        var pages = new List<List<string>>();
        var current = new List<string>();

        foreach (var line in lines)
        {
            current.Add(line);
            if (current.Count >= maxLinesPerPage)
            {
                pages.Add(current);
                current = new List<string>();
            }
        }

        if (current.Count > 0 || pages.Count == 0)
        {
            pages.Add(current);
        }

        var objectContents = new List<byte[]>();
        var pageObjectNumbers = new List<int>();
        var contentObjectNumbers = new List<int>();

        var nextObjectNumber = 3;
        foreach (var _ in pages)
        {
            pageObjectNumbers.Add(nextObjectNumber++);
            contentObjectNumbers.Add(nextObjectNumber++);
        }

        var fontObjectNumber = nextObjectNumber++;
        var kids = string.Join(" ", pageObjectNumbers.Select(x => $"{x} 0 R"));
        var pagesObject = $"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>";

        objectContents.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objectContents.Add(Encoding.ASCII.GetBytes(pagesObject));

        for (var i = 0; i < pages.Count; i++)
        {
            var pageObject = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontObjectNumber} 0 R >> >> /Contents {contentObjectNumbers[i]} 0 R >>";
            objectContents.Add(Encoding.ASCII.GetBytes(pageObject));

            var streamBuilder = new StringBuilder();
            streamBuilder.AppendLine("BT");
            streamBuilder.AppendLine("/F1 9 Tf");
            streamBuilder.AppendLine("11 TL");
            streamBuilder.AppendLine("40 800 Td");

            foreach (var line in pages[i])
            {
                streamBuilder.Append('(').Append(EscapePdfText(ToAscii(line))).AppendLine(") Tj");
                streamBuilder.AppendLine("T*");
            }

            streamBuilder.AppendLine("ET");

            var streamText = streamBuilder.ToString();
            var streamBytes = Encoding.ASCII.GetBytes(streamText);
            var contentPrefix = Encoding.ASCII.GetBytes($"<< /Length {streamBytes.Length} >>\nstream\n");
            var contentSuffix = Encoding.ASCII.GetBytes("endstream");

            using var contentStream = new MemoryStream();
            contentStream.Write(contentPrefix);
            contentStream.Write(streamBytes);
            contentStream.Write(contentSuffix);
            objectContents.Add(contentStream.ToArray());
        }

        objectContents.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        using var pdf = new MemoryStream();
        var offsets = new List<long> { 0 };

        WriteAscii(pdf, "%PDF-1.4\n");

        for (var i = 0; i < objectContents.Count; i++)
        {
            offsets.Add(pdf.Position);
            WriteAscii(pdf, $"{i + 1} 0 obj\n");
            pdf.Write(objectContents[i], 0, objectContents[i].Length);
            WriteAscii(pdf, "\nendobj\n");
        }

        var xrefOffset = pdf.Position;
        WriteAscii(pdf, $"xref\n0 {objectContents.Count + 1}\n");
        WriteAscii(pdf, "0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(pdf, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(pdf, $"trailer\n<< /Size {objectContents.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return pdf.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string EscapePdfText(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static string ToAscii(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("İ", "I", StringComparison.Ordinal)
            .Replace("I", "I", StringComparison.Ordinal)
            .Replace("ı", "i", StringComparison.Ordinal)
            .Replace("Ş", "S", StringComparison.Ordinal)
            .Replace("ş", "s", StringComparison.Ordinal)
            .Replace("Ğ", "G", StringComparison.Ordinal)
            .Replace("ğ", "g", StringComparison.Ordinal)
            .Replace("Ü", "U", StringComparison.Ordinal)
            .Replace("ü", "u", StringComparison.Ordinal)
            .Replace("Ö", "O", StringComparison.Ordinal)
            .Replace("ö", "o", StringComparison.Ordinal)
            .Replace("Ç", "C", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal);
    }

    private static string BuildSafeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return sanitized.Replace(' ', '_');
    }
}



