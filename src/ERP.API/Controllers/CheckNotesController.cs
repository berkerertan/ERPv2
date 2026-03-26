using ERP.API.Common;
using ERP.API.Contracts.Accounting;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/accounting/check-notes")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class CheckNotesController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CheckNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CheckNoteDto>>> GetCheckNotes(
        [FromQuery] string? q,
        [FromQuery] CheckNoteType? type,
        [FromQuery] CheckNoteDirection? direction,
        [FromQuery] CheckNoteStatus? status,
        [FromQuery] Guid? cariAccountId,
        [FromQuery] DateTime? startDueDateUtc,
        [FromQuery] DateTime? endDueDateUtc,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CheckNotes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Code.ToLower().Contains(term) ||
                (x.SerialNo != null && x.SerialNo.ToLower().Contains(term)) ||
                (x.BankName != null && x.BankName.ToLower().Contains(term)));
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (direction.HasValue)
        {
            query = query.Where(x => x.Direction == direction.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (cariAccountId.HasValue)
        {
            query = query.Where(x => x.CariAccountId == cariAccountId.Value);
        }

        if (startDueDateUtc.HasValue)
        {
            query = query.Where(x => x.DueDateUtc >= startDueDateUtc.Value);
        }

        if (endDueDateUtc.HasValue)
        {
            query = query.Where(x => x.DueDateUtc <= endDueDateUtc.Value);
        }

        var rows = await query
            .OrderByDescending(x => x.DueDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var cariIds = rows.Select(x => x.CariAccountId).Distinct().ToList();
        var cariMap = await dbContext.CariAccounts.AsNoTracking()
            .Where(x => cariIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var result = rows.Select(x => MapCheckNote(x, cariMap.GetValueOrDefault(x.CariAccountId))).ToList();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CheckNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckNoteDto>> GetCheckNoteById(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.CheckNotes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        var cari = await dbContext.CariAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.CariAccountId, cancellationToken);
        return Ok(MapCheckNote(row, cari));
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateCheckNote([FromBody] UpsertCheckNoteRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateUpsertRequestAsync(request, checkNoteId: null, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Error);
        }

        var code = request.Code.Trim();

        var row = new CheckNote
        {
            Code = code,
            Type = request.Type,
            Direction = request.Direction,
            Status = CheckNoteStatus.Portfolio,
            CariAccountId = request.CariAccountId,
            Amount = request.Amount,
            Currency = NormalizeCurrency(request.Currency),
            IssueDateUtc = request.IssueDateUtc == default ? DateTime.UtcNow : request.IssueDateUtc,
            DueDateUtc = request.DueDateUtc,
            BankName = TrimOrNull(request.BankName),
            BranchName = TrimOrNull(request.BranchName),
            AccountNo = TrimOrNull(request.AccountNo),
            SerialNo = TrimOrNull(request.SerialNo),
            Description = TrimOrNull(request.Description)
        };

        dbContext.CheckNotes.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/accounting/check-notes/{row.Id}", row.Id);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCheckNote(Guid id, [FromBody] UpsertCheckNoteRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.CheckNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        if (row.Status is CheckNoteStatus.Collected or CheckNoteStatus.Paid or CheckNoteStatus.Cancelled)
        {
            return BadRequest("Settled/cancelled check notes cannot be updated.");
        }

        var validation = await ValidateUpsertRequestAsync(request, checkNoteId: id, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Error);
        }

        row.Code = request.Code.Trim();
        row.Type = request.Type;
        row.Direction = request.Direction;
        row.CariAccountId = request.CariAccountId;
        row.Amount = request.Amount;
        row.Currency = NormalizeCurrency(request.Currency);
        row.IssueDateUtc = request.IssueDateUtc == default ? row.IssueDateUtc : request.IssueDateUtc;
        row.DueDateUtc = request.DueDateUtc;
        row.BankName = TrimOrNull(request.BankName);
        row.BranchName = TrimOrNull(request.BranchName);
        row.AccountNo = TrimOrNull(request.AccountNo);
        row.SerialNo = TrimOrNull(request.SerialNo);
        row.Description = TrimOrNull(request.Description);
        row.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCheckNote(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.CheckNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        if (row.Status is CheckNoteStatus.Collected or CheckNoteStatus.Paid)
        {
            return Conflict("Settled check note cannot be deleted.");
        }

        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCheckNoteStatus(Guid id, [FromBody] UpdateCheckNoteStatusRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.CheckNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        if (request.Status is CheckNoteStatus.Collected or CheckNoteStatus.Paid)
        {
            return BadRequest("Use settle endpoint for collected/paid transitions.");
        }

        if (row.Status is CheckNoteStatus.Collected or CheckNoteStatus.Paid or CheckNoteStatus.Cancelled)
        {
            return BadRequest("Finalized check note status cannot be changed.");
        }

        row.Status = request.Status;
        row.LastActionNote = TrimOrNull(request.Note);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/settle")]
    [ProducesResponseType(typeof(SettleCheckNoteResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SettleCheckNoteResultDto>> SettleCheckNote(Guid id, [FromBody] SettleCheckNoteRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.CheckNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        if (row.Status is CheckNoteStatus.Collected or CheckNoteStatus.Paid or CheckNoteStatus.Cancelled)
        {
            return BadRequest("Check note is already finalized.");
        }

        var cari = await dbContext.CariAccounts.FirstOrDefaultAsync(x => x.Id == row.CariAccountId, cancellationToken);
        if (cari is null)
        {
            return BadRequest("Cari account not found.");
        }

        if (row.Direction == CheckNoteDirection.Receivable && cari.Type == CariType.Supplier)
        {
            return Conflict("Receivable check/senet requires buyer/both cari type.");
        }

        if (row.Direction == CheckNoteDirection.Payable && cari.Type == CariType.BuyerBch)
        {
            return Conflict("Payable check/senet requires supplier/both cari type.");
        }

        await using var trx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var movementType = row.Direction == CheckNoteDirection.Receivable
            ? FinanceMovementType.Collection
            : FinanceMovementType.Payment;

        var movementDateUtc = request.TransactionDateUtc ?? DateTime.UtcNow;
        var movement = new FinanceMovement
        {
            CariAccountId = cari.Id,
            Type = movementType,
            Amount = row.Amount,
            MovementDateUtc = movementDateUtc,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? $"Settlement of {row.Code}"
                : request.Description.Trim(),
            ReferenceNo = string.IsNullOrWhiteSpace(request.ReferenceNo) ? row.Code : request.ReferenceNo.Trim()
        };

        dbContext.FinanceMovements.Add(movement);

        if (movementType == FinanceMovementType.Collection)
        {
            cari.CurrentBalance -= row.Amount;
        }
        else
        {
            cari.CurrentBalance += row.Amount;
        }

        Guid? cashTransactionId = null;
        Guid? bankTransactionId = null;
        decimal treasuryBalance;

        if (request.Channel == TreasuryChannel.Cash)
        {
            var cashAccount = await dbContext.CashAccounts.FirstOrDefaultAsync(x => x.Id == request.TreasuryAccountId, cancellationToken);
            if (cashAccount is null)
            {
                return BadRequest("Cash account not found.");
            }

            var txType = movementType == FinanceMovementType.Collection
                ? CashTransactionType.Collection
                : CashTransactionType.Payment;

            cashAccount.Balance += GetCashBalanceEffect(txType, row.Amount);

            var tx = new CashTransaction
            {
                CashAccountId = cashAccount.Id,
                CariAccountId = cari.Id,
                FinanceMovementId = movement.Id,
                Type = txType,
                Amount = row.Amount,
                TransactionDateUtc = movementDateUtc,
                Description = movement.Description,
                ReferenceNo = movement.ReferenceNo
            };

            dbContext.CashTransactions.Add(tx);
            cashTransactionId = tx.Id;
            treasuryBalance = cashAccount.Balance;
        }
        else
        {
            var bankAccount = await dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.TreasuryAccountId, cancellationToken);
            if (bankAccount is null)
            {
                return BadRequest("Bank account not found.");
            }

            var txType = movementType == FinanceMovementType.Collection
                ? BankTransactionType.Collection
                : BankTransactionType.Payment;

            bankAccount.Balance += GetBankBalanceEffect(txType, row.Amount);

            var tx = new BankTransaction
            {
                BankAccountId = bankAccount.Id,
                CariAccountId = cari.Id,
                FinanceMovementId = movement.Id,
                Type = txType,
                Amount = row.Amount,
                TransactionDateUtc = movementDateUtc,
                Description = movement.Description,
                ReferenceNo = movement.ReferenceNo
            };

            dbContext.BankTransactions.Add(tx);
            bankTransactionId = tx.Id;
            treasuryBalance = bankAccount.Balance;
        }

        row.Status = row.Direction == CheckNoteDirection.Receivable
            ? CheckNoteStatus.Collected
            : CheckNoteStatus.Paid;
        row.SettledAtUtc = movementDateUtc;
        row.RelatedFinanceMovementId = movement.Id;
        row.LastActionNote = string.IsNullOrWhiteSpace(request.Description) ? row.LastActionNote : request.Description.Trim();
        row.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);

        return Ok(new SettleCheckNoteResultDto(
            row.Id,
            row.Status,
            movement.Id,
            cashTransactionId,
            bankTransactionId,
            cari.CurrentBalance,
            treasuryBalance));
    }

    [HttpGet("due-list")]
    [ProducesResponseType(typeof(IReadOnlyList<CheckNoteDueListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CheckNoteDueListItemDto>>> GetCheckNoteDueList(
        [FromQuery] CheckNoteDirection? direction,
        [FromQuery] DateTime? startDueDateUtc,
        [FromQuery] DateTime? endDueDateUtc,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CheckNotes.AsNoTracking()
            .Where(x => x.Status == CheckNoteStatus.Portfolio || x.Status == CheckNoteStatus.Endorsed || x.Status == CheckNoteStatus.Protested);

        if (direction.HasValue)
        {
            query = query.Where(x => x.Direction == direction.Value);
        }

        if (startDueDateUtc.HasValue)
        {
            query = query.Where(x => x.DueDateUtc >= startDueDateUtc.Value);
        }

        if (endDueDateUtc.HasValue)
        {
            query = query.Where(x => x.DueDateUtc <= endDueDateUtc.Value);
        }

        var rows = await query.OrderBy(x => x.DueDateUtc).ToListAsync(cancellationToken);
        var cariIds = rows.Select(x => x.CariAccountId).Distinct().ToList();
        var cariMap = await dbContext.CariAccounts.AsNoTracking()
            .Where(x => cariIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var result = rows.Select(x =>
        {
            var dueDate = DateOnly.FromDateTime(x.DueDateUtc.Date);
            var remainingDays = dueDate.DayNumber - today.DayNumber;
            var cari = cariMap.GetValueOrDefault(x.CariAccountId);

            return new CheckNoteDueListItemDto(
                x.Id,
                x.Code,
                x.Type,
                x.Direction,
                x.Status,
                x.CariAccountId,
                cari?.Code ?? string.Empty,
                cari?.Name ?? string.Empty,
                x.Amount,
                x.Currency,
                dueDate,
                remainingDays);
        }).ToList();

        return Ok(result);
    }

    private async Task<(bool IsValid, string? Error)> ValidateUpsertRequestAsync(
        UpsertCheckNoteRequest request,
        Guid? checkNoteId,
        CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, "Code is required.");
        }

        if (request.Amount <= 0)
        {
            return (false, "Amount must be greater than zero.");
        }

        if (request.DueDateUtc == default)
        {
            return (false, "DueDateUtc is required.");
        }

        if (request.IssueDateUtc != default && request.DueDateUtc.Date < request.IssueDateUtc.Date)
        {
            return (false, "DueDateUtc cannot be earlier than IssueDateUtc.");
        }

        var codeExists = await dbContext.CheckNotes.AnyAsync(
            x => x.Code.ToLower() == code.ToLower() && (!checkNoteId.HasValue || x.Id != checkNoteId.Value),
            cancellationToken);

        if (codeExists)
        {
            return (false, "Check/Senet code already exists.");
        }

        var cariExists = await dbContext.CariAccounts.AnyAsync(x => x.Id == request.CariAccountId, cancellationToken);
        if (!cariExists)
        {
            return (false, "Cari account not found.");
        }

        return (true, null);
    }

    private static CheckNoteDto MapCheckNote(CheckNote x, CariAccount? cari)
        => new(
            x.Id,
            x.Code,
            x.Type,
            x.Direction,
            x.Status,
            x.CariAccountId,
            cari?.Code ?? string.Empty,
            cari?.Name ?? string.Empty,
            x.Amount,
            x.Currency,
            x.IssueDateUtc,
            x.DueDateUtc,
            x.BankName,
            x.BranchName,
            x.AccountNo,
            x.SerialNo,
            x.Description,
            x.LastActionNote,
            x.RelatedFinanceMovementId,
            x.SettledAtUtc,
            x.CreatedAtUtc);

    private static decimal GetCashBalanceEffect(CashTransactionType type, decimal amount)
        => type == CashTransactionType.Collection || type == CashTransactionType.TransferIn
            ? amount
            : -amount;

    private static decimal GetBankBalanceEffect(BankTransactionType type, decimal amount)
        => type == BankTransactionType.Collection || type == BankTransactionType.TransferIn
            ? amount
            : -amount;

    private static string NormalizeCurrency(string? currency)
        => string.IsNullOrWhiteSpace(currency)
            ? "TRY"
            : currency.Trim().ToUpperInvariant();

    private static string? TrimOrNull(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
