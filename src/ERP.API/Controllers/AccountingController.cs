using ERP.API.Common;
using ERP.API.Contracts.Accounting;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/accounting")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class AccountingController(ErpDbContext dbContext) : ControllerBase
{
    private const decimal Tolerance = 0.01m;

    [HttpGet("chart-of-accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<ChartOfAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ChartOfAccountDto>>> GetChartOfAccounts(
        [FromQuery] string? q,
        [FromQuery] AccountType? type,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ChartOfAccounts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(term) || x.Name.ToLower().Contains(term));
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var rows = await query
            .OrderBy(x => x.Code)
            .Select(x => new ChartOfAccountDto(x.Id, x.Code, x.Name, x.Type, x.IsActive, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet("chart-of-accounts/{id:guid}")]
    [ProducesResponseType(typeof(ChartOfAccountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChartOfAccountDto>> GetChartOfAccountById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ChartOfAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        return Ok(MapChartOfAccount(entity));
    }

    [HttpPost("chart-of-accounts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateChartOfAccount([FromBody] UpsertChartOfAccountRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Code and name are required.");
        }

        if (await dbContext.ChartOfAccounts.AnyAsync(x => x.Code.ToLower() == code.ToLower(), cancellationToken))
        {
            return Conflict("Chart of account code already exists.");
        }

        var entity = new ChartOfAccount
        {
            Code = code,
            Name = request.Name.Trim(),
            Type = request.Type,
            IsActive = request.IsActive
        };

        dbContext.ChartOfAccounts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/accounting/chart-of-accounts/{entity.Id}", entity.Id);
    }

    [HttpPut("chart-of-accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateChartOfAccount(Guid id, [FromBody] UpsertChartOfAccountRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ChartOfAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Code and name are required.");
        }

        if (await dbContext.ChartOfAccounts.AnyAsync(x => x.Id != id && x.Code.ToLower() == code.ToLower(), cancellationToken))
        {
            return Conflict("Chart of account code already exists.");
        }

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Type = request.Type;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("chart-of-accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteChartOfAccount(Guid id, CancellationToken cancellationToken)
    {
        var inUse = await dbContext.JournalEntryLines.AnyAsync(x => x.ChartOfAccountId == id, cancellationToken);
        if (inUse)
        {
            return Conflict("This account is used in journal lines and cannot be deleted.");
        }

        var entity = await dbContext.ChartOfAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("journal-entries")]
    [ProducesResponseType(typeof(IReadOnlyList<JournalEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JournalEntryDto>>> GetJournalEntries(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        [FromQuery] JournalEntryStatus? status,
        [FromQuery] bool includeLines,
        CancellationToken cancellationToken)
    {
        var query = dbContext.JournalEntries.AsNoTracking().AsQueryable();

        if (startDateUtc.HasValue)
        {
            query = query.Where(x => x.EntryDateUtc >= startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            query = query.Where(x => x.EntryDateUtc <= endDateUtc.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var entries = await query
            .OrderByDescending(x => x.EntryDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var entryIds = entries.Select(x => x.Id).ToList();
        var lines = includeLines || entryIds.Count > 0
            ? await dbContext.JournalEntryLines
                .AsNoTracking()
                .Where(x => entryIds.Contains(x.JournalEntryId))
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken)
            : [];

        var accountIds = lines.Select(x => x.ChartOfAccountId).Distinct().ToList();
        var accountMap = accountIds.Count > 0
            ? await dbContext.ChartOfAccounts
                .AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken)
            : new Dictionary<Guid, ChartOfAccount>();

        var byEntry = lines.GroupBy(x => x.JournalEntryId).ToDictionary(x => x.Key, x => x.ToList());

        var result = entries
            .Select(entry => MapJournalEntry(entry, byEntry.GetValueOrDefault(entry.Id) ?? [], accountMap, includeLines))
            .ToList();

        return Ok(result);
    }

    [HttpGet("journal-entries/{id:guid}")]
    [ProducesResponseType(typeof(JournalEntryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JournalEntryDto>> GetJournalEntryById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.JournalEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        var lines = await dbContext.JournalEntryLines
            .AsNoTracking()
            .Where(x => x.JournalEntryId == id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var accountIds = lines.Select(x => x.ChartOfAccountId).Distinct().ToList();
        var accountMap = accountIds.Count > 0
            ? await dbContext.ChartOfAccounts
                .AsNoTracking()
                .Where(x => accountIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken)
            : new Dictionary<Guid, ChartOfAccount>();

        return Ok(MapJournalEntry(entry, lines, accountMap, includeLines: true));
    }

    [HttpPost("journal-entries")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateJournalEntry([FromBody] UpsertJournalEntryRequest request, CancellationToken cancellationToken)
    {
        var validate = await ValidateJournalLinesAsync(request.Lines, cancellationToken);
        if (!validate.IsValid)
        {
            return BadRequest(validate.Error);
        }

        var voucherNo = string.IsNullOrWhiteSpace(request.VoucherNo)
            ? await GenerateVoucherNoAsync(cancellationToken)
            : request.VoucherNo.Trim();

        if (await dbContext.JournalEntries.AnyAsync(x => x.VoucherNo.ToLower() == voucherNo.ToLower(), cancellationToken))
        {
            return Conflict("Voucher number already exists.");
        }

        var status = request.PostOnCreate ? JournalEntryStatus.Posted : JournalEntryStatus.Draft;
        if (status == JournalEntryStatus.Posted && Math.Abs(validate.TotalDebit - validate.TotalCredit) > Tolerance)
        {
            return BadRequest("Posted journal entry must be balanced.");
        }

        var entry = new JournalEntry
        {
            VoucherNo = voucherNo,
            EntryDateUtc = request.EntryDateUtc == default ? DateTime.UtcNow : request.EntryDateUtc,
            Status = status,
            Description = request.Description,
            Lines = request.Lines.Select(x => new JournalEntryLine
            {
                ChartOfAccountId = x.ChartOfAccountId,
                Debit = x.Debit,
                Credit = x.Credit,
                Description = x.Description
            }).ToList()
        };

        dbContext.JournalEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/accounting/journal-entries/{entry.Id}", entry.Id);
    }

    [HttpPut("journal-entries/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateJournalEntry(Guid id, [FromBody] UpsertJournalEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.JournalEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        if (entry.Status != JournalEntryStatus.Draft)
        {
            return BadRequest("Only draft journal entries can be updated.");
        }

        var validate = await ValidateJournalLinesAsync(request.Lines, cancellationToken);
        if (!validate.IsValid)
        {
            return BadRequest(validate.Error);
        }

        var voucherNo = string.IsNullOrWhiteSpace(request.VoucherNo)
            ? entry.VoucherNo
            : request.VoucherNo.Trim();

        if (await dbContext.JournalEntries.AnyAsync(x => x.Id != id && x.VoucherNo.ToLower() == voucherNo.ToLower(), cancellationToken))
        {
            return Conflict("Voucher number already exists.");
        }

        entry.VoucherNo = voucherNo;
        entry.EntryDateUtc = request.EntryDateUtc == default ? entry.EntryDateUtc : request.EntryDateUtc;
        entry.Description = request.Description;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var line in entry.Lines)
        {
            line.MarkAsDeleted();
        }

        var newLines = request.Lines.Select(x => new JournalEntryLine
        {
            JournalEntryId = entry.Id,
            ChartOfAccountId = x.ChartOfAccountId,
            Debit = x.Debit,
            Credit = x.Credit,
            Description = x.Description
        }).ToList();

        dbContext.JournalEntryLines.AddRange(newLines);

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("journal-entries/{id:guid}/post")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PostJournalEntry(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.JournalEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        if (entry.Status != JournalEntryStatus.Draft)
        {
            return BadRequest("Only draft journal entries can be posted.");
        }

        if (entry.Lines.Count == 0)
        {
            return BadRequest("Journal entry must have at least one line.");
        }

        var totalDebit = entry.Lines.Sum(x => x.Debit);
        var totalCredit = entry.Lines.Sum(x => x.Credit);

        if (Math.Abs(totalDebit - totalCredit) > Tolerance)
        {
            return BadRequest("Journal entry is not balanced.");
        }

        entry.Status = JournalEntryStatus.Posted;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("journal-entries/{id:guid}/reverse")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> ReverseJournalEntry(Guid id, [FromBody] ReverseJournalEntryRequest? request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.JournalEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        if (entry.Status != JournalEntryStatus.Posted)
        {
            return BadRequest("Only posted journal entries can be reversed.");
        }

        var voucherNo = string.IsNullOrWhiteSpace(request?.VoucherNo)
            ? await GenerateReverseVoucherNoAsync(entry.VoucherNo, cancellationToken)
            : request.VoucherNo.Trim();

        if (await dbContext.JournalEntries.AnyAsync(x => x.VoucherNo.ToLower() == voucherNo.ToLower(), cancellationToken))
        {
            return Conflict("Reverse voucher number already exists.");
        }

        var reverseEntry = new JournalEntry
        {
            VoucherNo = voucherNo,
            EntryDateUtc = DateTime.UtcNow,
            Status = JournalEntryStatus.Posted,
            Description = string.IsNullOrWhiteSpace(request?.Description)
                ? $"Reversal of {entry.VoucherNo}"
                : request!.Description!.Trim(),
            Lines = entry.Lines.Select(x => new JournalEntryLine
            {
                ChartOfAccountId = x.ChartOfAccountId,
                Debit = x.Credit,
                Credit = x.Debit,
                Description = x.Description
            }).ToList()
        };

        entry.Status = JournalEntryStatus.Reversed;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.JournalEntries.Add(reverseEntry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/accounting/journal-entries/{reverseEntry.Id}", reverseEntry.Id);
    }

    [HttpDelete("journal-entries/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteJournalEntry(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.JournalEntries
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        if (entry.Status != JournalEntryStatus.Draft)
        {
            return BadRequest("Only draft journal entries can be deleted.");
        }

        entry.MarkAsDeleted();
        foreach (var line in entry.Lines)
        {
            line.MarkAsDeleted();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("cash-accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<CashAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CashAccountDto>>> GetCashAccounts(CancellationToken cancellationToken)
    {
        var rows = await dbContext.CashAccounts
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new CashAccountDto(x.Id, x.Code, x.Name, x.Currency, x.Balance, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet("cash-accounts/{id:guid}")]
    [ProducesResponseType(typeof(CashAccountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CashAccountDto>> GetCashAccountById(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.CashAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return Ok(MapCashAccount(row));
    }

    [HttpPost("cash-accounts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateCashAccount([FromBody] UpsertCashAccountRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Code and name are required.");
        }

        if (await dbContext.CashAccounts.AnyAsync(x => x.Code.ToLower() == code.ToLower(), cancellationToken))
        {
            return Conflict("Cash account code already exists.");
        }

        var row = new CashAccount
        {
            Code = code,
            Name = request.Name.Trim(),
            Currency = NormalizeCurrency(request.Currency),
            Balance = 0m
        };

        dbContext.CashAccounts.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/accounting/cash-accounts/{row.Id}", row.Id);
    }

    [HttpPut("cash-accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCashAccount(Guid id, [FromBody] UpsertCashAccountRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.CashAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Code and name are required.");
        }

        if (await dbContext.CashAccounts.AnyAsync(x => x.Id != id && x.Code.ToLower() == code.ToLower(), cancellationToken))
        {
            return Conflict("Cash account code already exists.");
        }

        row.Code = code;
        row.Name = request.Name.Trim();
        row.Currency = NormalizeCurrency(request.Currency);
        row.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("cash-accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCashAccount(Guid id, CancellationToken cancellationToken)
    {
        var hasMovements = await dbContext.CashTransactions.AnyAsync(x => x.CashAccountId == id, cancellationToken);
        if (hasMovements)
        {
            return Conflict("Cash account has transactions and cannot be deleted.");
        }

        var row = await dbContext.CashAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("cash-transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<CashTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CashTransactionDto>>> GetCashTransactions(
        [FromQuery] Guid? cashAccountId,
        [FromQuery] Guid? cariAccountId,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CashTransactions.AsNoTracking().AsQueryable();

        if (cashAccountId.HasValue)
        {
            query = query.Where(x => x.CashAccountId == cashAccountId.Value);
        }

        if (cariAccountId.HasValue)
        {
            query = query.Where(x => x.CariAccountId == cariAccountId.Value);
        }

        if (startDateUtc.HasValue)
        {
            query = query.Where(x => x.TransactionDateUtc >= startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            query = query.Where(x => x.TransactionDateUtc <= endDateUtc.Value);
        }

        var rows = await query
            .OrderByDescending(x => x.TransactionDateUtc)
            .Select(x => new CashTransactionDto(x.Id, x.CashAccountId, x.CariAccountId, x.FinanceMovementId, x.Type, x.Amount, x.TransactionDateUtc, x.Description, x.ReferenceNo, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("cash-accounts/{cashAccountId:guid}/transactions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateCashTransaction(Guid cashAccountId, [FromBody] CreateCashTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        var account = await dbContext.CashAccounts.FirstOrDefaultAsync(x => x.Id == cashAccountId, cancellationToken);
        if (account is null)
        {
            return NotFound("Cash account not found.");
        }

        if (request.CariAccountId.HasValue)
        {
            var cariExists = await dbContext.CariAccounts.AnyAsync(x => x.Id == request.CariAccountId.Value, cancellationToken);
            if (!cariExists)
            {
                return BadRequest("Cari account not found.");
            }
        }

        if (request.FinanceMovementId.HasValue)
        {
            var movementExists = await dbContext.FinanceMovements.AnyAsync(x => x.Id == request.FinanceMovementId.Value, cancellationToken);
            if (!movementExists)
            {
                return BadRequest("Finance movement not found.");
            }
        }

        account.Balance += GetCashBalanceEffect(request.Type, request.Amount);

        var tx = new CashTransaction
        {
            CashAccountId = cashAccountId,
            CariAccountId = request.CariAccountId,
            FinanceMovementId = request.FinanceMovementId,
            Type = request.Type,
            Amount = request.Amount,
            TransactionDateUtc = request.TransactionDateUtc ?? DateTime.UtcNow,
            Description = request.Description,
            ReferenceNo = request.ReferenceNo
        };

        dbContext.CashTransactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/accounting/cash-transactions/{tx.Id}", tx.Id);
    }

    [HttpDelete("cash-transactions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCashTransaction(Guid id, CancellationToken cancellationToken)
    {
        var tx = await dbContext.CashTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tx is null)
        {
            return NotFound();
        }

        var account = await dbContext.CashAccounts.FirstOrDefaultAsync(x => x.Id == tx.CashAccountId, cancellationToken);
        if (account is not null)
        {
            account.Balance -= GetCashBalanceEffect(tx.Type, tx.Amount);
        }

        if (tx.FinanceMovementId.HasValue)
        {
            await TryRollbackLinkedFinanceMovementAsync(tx.FinanceMovementId.Value, tx.Id, cancellationToken);
        }

        tx.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("cash-transactions/{cashTransactionId:guid}/match-finance-movement/{financeMovementId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MatchCashTransaction(Guid cashTransactionId, Guid financeMovementId, CancellationToken cancellationToken)
    {
        var tx = await dbContext.CashTransactions.FirstOrDefaultAsync(x => x.Id == cashTransactionId, cancellationToken);
        if (tx is null)
        {
            return NotFound("Cash transaction not found.");
        }

        var movement = await dbContext.FinanceMovements.FirstOrDefaultAsync(x => x.Id == financeMovementId, cancellationToken);
        if (movement is null)
        {
            return NotFound("Finance movement not found.");
        }

        if (Math.Abs(tx.Amount - movement.Amount) > Tolerance)
        {
            return BadRequest("Amount mismatch between cash transaction and finance movement.");
        }

        tx.FinanceMovementId = movement.Id;
        tx.CariAccountId ??= movement.CariAccountId;
        tx.ReferenceNo ??= movement.ReferenceNo;
        tx.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("bank-accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<BankAccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BankAccountDto>>> GetBankAccounts(CancellationToken cancellationToken)
    {
        var rows = await dbContext.BankAccounts
            .AsNoTracking()
            .OrderBy(x => x.BankName)
            .ThenBy(x => x.Iban)
            .Select(x => new BankAccountDto(x.Id, x.BankName, x.BranchName, x.Iban, x.Currency, x.Balance, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet("bank-accounts/{id:guid}")]
    [ProducesResponseType(typeof(BankAccountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BankAccountDto>> GetBankAccountById(Guid id, CancellationToken cancellationToken)
    {
        var row = await dbContext.BankAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        return Ok(MapBankAccount(row));
    }

    [HttpPost("bank-accounts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateBankAccount([FromBody] UpsertBankAccountRequest request, CancellationToken cancellationToken)
    {
        var iban = NormalizeIban(request.Iban);
        if (string.IsNullOrWhiteSpace(request.BankName) || string.IsNullOrWhiteSpace(iban))
        {
            return BadRequest("Bank name and IBAN are required.");
        }

        if (await dbContext.BankAccounts.AnyAsync(x => x.Iban.ToUpper() == iban, cancellationToken))
        {
            return Conflict("IBAN already exists.");
        }

        var row = new BankAccount
        {
            BankName = request.BankName.Trim(),
            BranchName = request.BranchName.Trim(),
            Iban = iban,
            Currency = NormalizeCurrency(request.Currency),
            Balance = 0m
        };

        dbContext.BankAccounts.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/accounting/bank-accounts/{row.Id}", row.Id);
    }

    [HttpPut("bank-accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateBankAccount(Guid id, [FromBody] UpsertBankAccountRequest request, CancellationToken cancellationToken)
    {
        var row = await dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        var iban = NormalizeIban(request.Iban);
        if (string.IsNullOrWhiteSpace(request.BankName) || string.IsNullOrWhiteSpace(iban))
        {
            return BadRequest("Bank name and IBAN are required.");
        }

        if (await dbContext.BankAccounts.AnyAsync(x => x.Id != id && x.Iban.ToUpper() == iban, cancellationToken))
        {
            return Conflict("IBAN already exists.");
        }

        row.BankName = request.BankName.Trim();
        row.BranchName = request.BranchName.Trim();
        row.Iban = iban;
        row.Currency = NormalizeCurrency(request.Currency);
        row.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("bank-accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBankAccount(Guid id, CancellationToken cancellationToken)
    {
        var hasMovements = await dbContext.BankTransactions.AnyAsync(x => x.BankAccountId == id, cancellationToken);
        if (hasMovements)
        {
            return Conflict("Bank account has transactions and cannot be deleted.");
        }

        var row = await dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
        {
            return NotFound();
        }

        row.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("bank-transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<BankTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BankTransactionDto>>> GetBankTransactions(
        [FromQuery] Guid? bankAccountId,
        [FromQuery] Guid? cariAccountId,
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BankTransactions.AsNoTracking().AsQueryable();

        if (bankAccountId.HasValue)
        {
            query = query.Where(x => x.BankAccountId == bankAccountId.Value);
        }

        if (cariAccountId.HasValue)
        {
            query = query.Where(x => x.CariAccountId == cariAccountId.Value);
        }

        if (startDateUtc.HasValue)
        {
            query = query.Where(x => x.TransactionDateUtc >= startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            query = query.Where(x => x.TransactionDateUtc <= endDateUtc.Value);
        }

        var rows = await query
            .OrderByDescending(x => x.TransactionDateUtc)
            .Select(x => new BankTransactionDto(x.Id, x.BankAccountId, x.CariAccountId, x.FinanceMovementId, x.Type, x.Amount, x.TransactionDateUtc, x.Description, x.ReferenceNo, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpPost("bank-accounts/{bankAccountId:guid}/transactions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> CreateBankTransaction(Guid bankAccountId, [FromBody] CreateBankTransactionRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        var account = await dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == bankAccountId, cancellationToken);
        if (account is null)
        {
            return NotFound("Bank account not found.");
        }

        if (request.CariAccountId.HasValue)
        {
            var cariExists = await dbContext.CariAccounts.AnyAsync(x => x.Id == request.CariAccountId.Value, cancellationToken);
            if (!cariExists)
            {
                return BadRequest("Cari account not found.");
            }
        }

        if (request.FinanceMovementId.HasValue)
        {
            var movementExists = await dbContext.FinanceMovements.AnyAsync(x => x.Id == request.FinanceMovementId.Value, cancellationToken);
            if (!movementExists)
            {
                return BadRequest("Finance movement not found.");
            }
        }

        account.Balance += GetBankBalanceEffect(request.Type, request.Amount);

        var tx = new BankTransaction
        {
            BankAccountId = bankAccountId,
            CariAccountId = request.CariAccountId,
            FinanceMovementId = request.FinanceMovementId,
            Type = request.Type,
            Amount = request.Amount,
            TransactionDateUtc = request.TransactionDateUtc ?? DateTime.UtcNow,
            Description = request.Description,
            ReferenceNo = request.ReferenceNo
        };

        dbContext.BankTransactions.Add(tx);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/accounting/bank-transactions/{tx.Id}", tx.Id);
    }

    [HttpDelete("bank-transactions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteBankTransaction(Guid id, CancellationToken cancellationToken)
    {
        var tx = await dbContext.BankTransactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tx is null)
        {
            return NotFound();
        }

        var account = await dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == tx.BankAccountId, cancellationToken);
        if (account is not null)
        {
            account.Balance -= GetBankBalanceEffect(tx.Type, tx.Amount);
        }

        if (tx.FinanceMovementId.HasValue)
        {
            await TryRollbackLinkedFinanceMovementAsync(tx.FinanceMovementId.Value, tx.Id, cancellationToken);
        }

        tx.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("bank-transactions/{bankTransactionId:guid}/match-finance-movement/{financeMovementId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MatchBankTransaction(Guid bankTransactionId, Guid financeMovementId, CancellationToken cancellationToken)
    {
        var tx = await dbContext.BankTransactions.FirstOrDefaultAsync(x => x.Id == bankTransactionId, cancellationToken);
        if (tx is null)
        {
            return NotFound("Bank transaction not found.");
        }

        var movement = await dbContext.FinanceMovements.FirstOrDefaultAsync(x => x.Id == financeMovementId, cancellationToken);
        if (movement is null)
        {
            return NotFound("Finance movement not found.");
        }

        if (Math.Abs(tx.Amount - movement.Amount) > Tolerance)
        {
            return BadRequest("Amount mismatch between bank transaction and finance movement.");
        }

        tx.FinanceMovementId = movement.Id;
        tx.CariAccountId ??= movement.CariAccountId;
        tx.ReferenceNo ??= movement.ReferenceNo;
        tx.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("collections-payments")]
    [ProducesResponseType(typeof(CollectionPaymentResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionPaymentResultDto>> CreateCollectionPayment([FromBody] CreateCollectionPaymentRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero.");
        }

        var cari = await dbContext.CariAccounts.FirstOrDefaultAsync(x => x.Id == request.CariAccountId, cancellationToken);
        if (cari is null)
        {
            return NotFound("Cari account not found.");
        }

        if (request.Type == FinanceMovementType.Collection && cari.Type == CariType.Supplier)
        {
            return Conflict("Collection can only be used for buyer/BCH or both cari types.");
        }

        if (request.Type == FinanceMovementType.Payment && cari.Type == CariType.BuyerBch)
        {
            return Conflict("Payment can only be used for supplier/both cari types.");
        }

        await using var trx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var movement = new FinanceMovement
        {
            CariAccountId = cari.Id,
            Type = request.Type,
            Amount = request.Amount,
            MovementDateUtc = request.TransactionDateUtc ?? DateTime.UtcNow,
            Description = request.Description,
            ReferenceNo = request.ReferenceNo
        };

        dbContext.FinanceMovements.Add(movement);

        if (request.Type == FinanceMovementType.Collection)
        {
            cari.CurrentBalance -= request.Amount;
        }
        else
        {
            cari.CurrentBalance += request.Amount;
        }

        Guid? cashTxId = null;
        Guid? bankTxId = null;
        decimal treasuryBalance;

        if (request.Channel == TreasuryChannel.Cash)
        {
            var cashAccount = await dbContext.CashAccounts.FirstOrDefaultAsync(x => x.Id == request.TreasuryAccountId, cancellationToken);
            if (cashAccount is null)
            {
                return BadRequest("Cash account not found.");
            }

            var txType = request.Type == FinanceMovementType.Collection
                ? CashTransactionType.Collection
                : CashTransactionType.Payment;

            cashAccount.Balance += GetCashBalanceEffect(txType, request.Amount);

            var tx = new CashTransaction
            {
                CashAccountId = cashAccount.Id,
                CariAccountId = cari.Id,
                FinanceMovementId = movement.Id,
                Type = txType,
                Amount = request.Amount,
                TransactionDateUtc = movement.MovementDateUtc,
                Description = request.Description,
                ReferenceNo = request.ReferenceNo
            };

            dbContext.CashTransactions.Add(tx);
            cashTxId = tx.Id;
            treasuryBalance = cashAccount.Balance;
        }
        else
        {
            var bankAccount = await dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.TreasuryAccountId, cancellationToken);
            if (bankAccount is null)
            {
                return BadRequest("Bank account not found.");
            }

            var txType = request.Type == FinanceMovementType.Collection
                ? BankTransactionType.Collection
                : BankTransactionType.Payment;

            bankAccount.Balance += GetBankBalanceEffect(txType, request.Amount);

            var tx = new BankTransaction
            {
                BankAccountId = bankAccount.Id,
                CariAccountId = cari.Id,
                FinanceMovementId = movement.Id,
                Type = txType,
                Amount = request.Amount,
                TransactionDateUtc = movement.MovementDateUtc,
                Description = request.Description,
                ReferenceNo = request.ReferenceNo
            };

            dbContext.BankTransactions.Add(tx);
            bankTxId = tx.Id;
            treasuryBalance = bankAccount.Balance;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);

        return Ok(new CollectionPaymentResultDto(
            movement.Id,
            cashTxId,
            bankTxId,
            cari.CurrentBalance,
            treasuryBalance));
    }

    private static ChartOfAccountDto MapChartOfAccount(ChartOfAccount x)
        => new(x.Id, x.Code, x.Name, x.Type, x.IsActive, x.CreatedAtUtc);

    private static JournalEntryDto MapJournalEntry(
        JournalEntry entry,
        IReadOnlyList<JournalEntryLine> lines,
        IReadOnlyDictionary<Guid, ChartOfAccount> accountMap,
        bool includeLines)
    {
        var mappedLines = includeLines
            ? lines.Select(x => MapJournalLine(x, accountMap.GetValueOrDefault(x.ChartOfAccountId))).ToList()
            : [];

        return new JournalEntryDto(
            entry.Id,
            entry.VoucherNo,
            entry.EntryDateUtc,
            entry.Status,
            entry.Description,
            lines.Sum(x => x.Debit),
            lines.Sum(x => x.Credit),
            mappedLines,
            entry.CreatedAtUtc);
    }

    private static JournalEntryLineDto MapJournalLine(JournalEntryLine line, ChartOfAccount? account)
        => new(
            line.Id,
            line.ChartOfAccountId,
            account?.Code ?? string.Empty,
            account?.Name ?? string.Empty,
            line.Debit,
            line.Credit,
            line.Description);

    private static CashAccountDto MapCashAccount(CashAccount x)
        => new(x.Id, x.Code, x.Name, x.Currency, x.Balance, x.CreatedAtUtc);

    private static CashTransactionDto MapCashTransaction(CashTransaction x)
        => new(
            x.Id,
            x.CashAccountId,
            x.CariAccountId,
            x.FinanceMovementId,
            x.Type,
            x.Amount,
            x.TransactionDateUtc,
            x.Description,
            x.ReferenceNo,
            x.CreatedAtUtc);

    private static BankAccountDto MapBankAccount(BankAccount x)
        => new(x.Id, x.BankName, x.BranchName, x.Iban, x.Currency, x.Balance, x.CreatedAtUtc);

    private static BankTransactionDto MapBankTransaction(BankTransaction x)
        => new(
            x.Id,
            x.BankAccountId,
            x.CariAccountId,
            x.FinanceMovementId,
            x.Type,
            x.Amount,
            x.TransactionDateUtc,
            x.Description,
            x.ReferenceNo,
            x.CreatedAtUtc);

    private async Task<(bool IsValid, string? Error, decimal TotalDebit, decimal TotalCredit)> ValidateJournalLinesAsync(
        IReadOnlyList<UpsertJournalEntryLineRequest> lines,
        CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
        {
            return (false, "Journal entry must have at least one line.", 0m, 0m);
        }

        var accountIds = lines.Select(x => x.ChartOfAccountId).Distinct().ToList();
        var accountCount = await dbContext.ChartOfAccounts.CountAsync(x => accountIds.Contains(x.Id), cancellationToken);
        if (accountCount != accountIds.Count)
        {
            return (false, "One or more chart of accounts were not found.", 0m, 0m);
        }

        foreach (var line in lines)
        {
            if (line.Debit < 0 || line.Credit < 0)
            {
                return (false, "Debit/Credit cannot be negative.", 0m, 0m);
            }

            if ((line.Debit <= 0 && line.Credit <= 0) || (line.Debit > 0 && line.Credit > 0))
            {
                return (false, "Each line must have only debit or only credit.", 0m, 0m);
            }
        }

        var totalDebit = lines.Sum(x => x.Debit);
        var totalCredit = lines.Sum(x => x.Credit);
        return (true, null, totalDebit, totalCredit);
    }

    private async Task<string> GenerateVoucherNoAsync(CancellationToken cancellationToken)
    {
        var stamp = DateTime.UtcNow;
        var prefix = $"JV-{stamp:yyyyMMdd}";

        for (var i = 1; i <= 9999; i++)
        {
            var candidate = $"{prefix}-{i:0000}";
            var exists = await dbContext.JournalEntries.AnyAsync(x => x.VoucherNo == candidate, cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        return $"JV-{stamp:yyyyMMddHHmmss}";
    }

    private async Task<string> GenerateReverseVoucherNoAsync(string baseVoucher, CancellationToken cancellationToken)
    {
        var normalized = string.IsNullOrWhiteSpace(baseVoucher) ? "REV" : baseVoucher.Trim();

        for (var i = 1; i <= 999; i++)
        {
            var candidate = $"{normalized}-REV{i:000}";
            var exists = await dbContext.JournalEntries.AnyAsync(x => x.VoucherNo == candidate, cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        return $"{normalized}-REV-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private async Task TryRollbackLinkedFinanceMovementAsync(Guid financeMovementId, Guid transactionId, CancellationToken cancellationToken)
    {
        var linkedInOtherCash = await dbContext.CashTransactions
            .AnyAsync(x => x.FinanceMovementId == financeMovementId && x.Id != transactionId, cancellationToken);
        var linkedInOtherBank = await dbContext.BankTransactions
            .AnyAsync(x => x.FinanceMovementId == financeMovementId && x.Id != transactionId, cancellationToken);

        if (linkedInOtherCash || linkedInOtherBank)
        {
            return;
        }

        var movement = await dbContext.FinanceMovements.FirstOrDefaultAsync(x => x.Id == financeMovementId, cancellationToken);
        if (movement is null)
        {
            return;
        }

        var cari = await dbContext.CariAccounts.FirstOrDefaultAsync(x => x.Id == movement.CariAccountId, cancellationToken);
        if (cari is not null)
        {
            if (movement.Type == FinanceMovementType.Collection)
            {
                cari.CurrentBalance += movement.Amount;
            }
            else
            {
                cari.CurrentBalance -= movement.Amount;
            }
        }

        movement.MarkAsDeleted();
        movement.UpdatedAtUtc = DateTime.UtcNow;
    }

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

    private static string NormalizeIban(string? iban)
        => string.IsNullOrWhiteSpace(iban)
            ? string.Empty
            : iban.Replace(" ", string.Empty).Trim().ToUpperInvariant();
}




