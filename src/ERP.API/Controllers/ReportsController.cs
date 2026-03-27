using ERP.API.Common;
using ERP.API.Contracts.Accounting;
using ERP.Application.Features.Reports.Queries.GetCariAging;
using ERP.Application.Features.Reports.Queries.GetCariBalances;
using ERP.Application.Features.Reports.Queries.GetIncomeExpenseSummary;
using ERP.Application.Features.Reports.Queries.GetPurchaseSummary;
using ERP.Application.Features.Reports.Queries.GetSalesSummary;
using ERP.Application.Features.StockMovements.Queries.GetStockBalances;
using ERP.Domain.Constants;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/reports")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class ReportsController(IMediator mediator, ErpDbContext dbContext) : ControllerBase
{
    [HttpGet("dashboard-summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        // Sales total — onaylı siparişlerin toplam tutarı
        var totalSales = await dbContext.SalesOrders
            .AsNoTracking()
            .Where(x => x.Status == OrderStatus.Approved)
            .SelectMany(x => x.Items)
            .SumAsync(i => (decimal?)( i.Quantity * i.UnitPrice), cancellationToken) ?? 0m;

        var totalOrderCount       = await dbContext.SalesOrders.CountAsync(cancellationToken);
        var totalProductCount     = await dbContext.Products.CountAsync(cancellationToken);
        var totalActiveCariCount  = await dbContext.CariAccounts.CountAsync(cancellationToken);

        // Kasa ve banka bakiyeleri
        var totalBankBalance = await dbContext.BankAccounts.AsNoTracking().SumAsync(x => (decimal?)x.Balance, cancellationToken) ?? 0m;
        var totalCashBalance = await dbContext.CashAccounts.AsNoTracking().SumAsync(x => (decimal?)x.Balance, cancellationToken) ?? 0m;

        // Vadesi GEÇMİŞ alacaklar (RemainingBalance > 0 VE dueDate < bugün)
        var now = DateTime.UtcNow;
        var overdueReceivables = await dbContext.CariDebtItems
            .AsNoTracking()
            .Where(x => x.RemainingBalance > 0 && x.TransactionDate < now)
            .SumAsync(x => (decimal?)x.RemainingBalance, cancellationToken) ?? 0m;

        // Vadesi GEÇMİŞ çek/senet sayısı (portföyde/ciro edilmiş ve dueDate geçmiş)
        var overdueCheckNoteCount = await dbContext.CheckNotes
            .AsNoTracking()
            .Where(x =>
                (x.Status == CheckNoteStatus.Portfolio || x.Status == CheckNoteStatus.Endorsed) &&
                x.DueDateUtc < now)
            .CountAsync(cancellationToken);

        // Bekleyen teklif sayısı
        var pendingQuoteCount = await dbContext.Quotes
            .AsNoTracking()
            .Where(x => x.Status == QuoteStatus.Sent || x.Status == QuoteStatus.Draft)
            .CountAsync(cancellationToken);

        return Ok(new DashboardSummaryDto(
            Math.Round(totalSales, 2),
            totalOrderCount,
            totalProductCount,
            totalActiveCariCount,
            Math.Round(totalBankBalance, 2),
            Math.Round(totalCashBalance, 2),
            Math.Round(overdueReceivables, 2),
            overdueCheckNoteCount,
            pendingQuoteCount));
    }

    [HttpGet("stock")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Reports)]
    [ProducesResponseType(typeof(IReadOnlyList<StockReportItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockReportItemDto>>> GetStock(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStockBalancesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("sales")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Reports)]
    [ProducesResponseType(typeof(IReadOnlyList<SalesReportItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SalesReportItemDto>>> GetSales(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetSalesSummaryQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("purchases")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Reports)]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseReportItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PurchaseReportItemDto>>> GetPurchases(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetPurchaseSummaryQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("cari-balances")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Reports)]
    [ProducesResponseType(typeof(IReadOnlyList<CariBalanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariBalanceDto>>> GetCariBalances(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariBalancesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("cari-aging")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Reports)]
    [ProducesResponseType(typeof(IReadOnlyList<CariAgingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CariAgingDto>>> GetCariAging(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariAgingQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("income-expense")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Reports)]
    [ProducesResponseType(typeof(IncomeExpenseSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IncomeExpenseSummaryDto>> GetIncomeExpense(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetIncomeExpenseSummaryQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("finance/cash-flow-forecast")]
    [RequireSubscriptionFeature(SubscriptionFeatures.Reports)]
    [ProducesResponseType(typeof(IReadOnlyList<CashFlowForecastDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CashFlowForecastDto>>> GetCashFlowForecast(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var horizonDays = Math.Clamp(days, 1, 365);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var endDate = today.AddDays(horizonDays - 1);

        var caris = await dbContext.CariAccounts
            .AsNoTracking()
            .Select(x => new { x.Id, x.Type, x.MaturityDays })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var items = await dbContext.CariDebtItems
            .AsNoTracking()
            .Where(x => x.RemainingBalance > 0)
            .ToListAsync(cancellationToken);

        var daily = new Dictionary<DateOnly, (decimal In, decimal Out)>();

        foreach (var item in items)
        {
            if (!caris.TryGetValue(item.CariAccountId, out var cari))
            {
                continue;
            }

            var maturityDays = Math.Max(0, cari.MaturityDays);
            var due = DateOnly.FromDateTime(item.TransactionDate.Date.AddDays(maturityDays));
            if (due < today || due > endDate)
            {
                continue;
            }

            var amount = Math.Max(0m, item.RemainingBalance);
            var current = daily.GetValueOrDefault(due);

            if (cari.Type == CariType.Supplier)
            {
                daily[due] = (current.In, current.Out + amount);
            }
            else
            {
                daily[due] = (current.In + amount, current.Out);
            }
        }

        var result = new List<CashFlowForecastDto>(horizonDays);
        for (var i = 0; i < horizonDays; i++)
        {
            var date = today.AddDays(i);
            var row = daily.GetValueOrDefault(date);
            result.Add(new CashFlowForecastDto(date, row.In, row.Out, row.In - row.Out));
        }

        return Ok(result);
    }

    [HttpGet("finance/due-list")]
    [ProducesResponseType(typeof(IReadOnlyList<DueListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DueListItemDto>>> GetDueList(
        [FromQuery] int days = 90,
        CancellationToken cancellationToken = default)
    {
        var horizonDays = Math.Clamp(days, 1, 365);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var endDate = today.AddDays(horizonDays);

        var caris = await dbContext.CariAccounts
            .AsNoTracking()
            .Select(x => new { x.Id, x.Code, x.Name, x.MaturityDays })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var items = await dbContext.CariDebtItems
            .AsNoTracking()
            .Where(x => x.RemainingBalance > 0)
            .ToListAsync(cancellationToken);

        var rows = new List<DueListItemDto>();

        foreach (var item in items)
        {
            if (!caris.TryGetValue(item.CariAccountId, out var cari))
            {
                continue;
            }

            var maturityDays = Math.Max(0, cari.MaturityDays);
            var due = DateOnly.FromDateTime(item.TransactionDate.Date.AddDays(maturityDays));
            if (due > endDate)
            {
                continue;
            }

            var overdueDays = due < today ? today.DayNumber - due.DayNumber : 0;

            rows.Add(new DueListItemDto(
                item.CariAccountId,
                cari.Code,
                cari.Name,
                due,
                Math.Round(item.RemainingBalance, 2),
                overdueDays));
        }

        return Ok(rows
            .OrderByDescending(x => x.OverdueDays)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.CariName)
            .ToList());
    }

    [HttpGet("finance/profitability/products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductProfitabilityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductProfitabilityDto>>> GetProductProfitability(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var salesOrders = await dbContext.SalesOrders
            .AsNoTracking()
            .Where(x => x.Status == OrderStatus.Approved)
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        if (startDateUtc.HasValue)
        {
            salesOrders = salesOrders.Where(x => x.OrderDateUtc >= startDateUtc.Value).ToList();
        }

        if (endDateUtc.HasValue)
        {
            salesOrders = salesOrders.Where(x => x.OrderDateUtc <= endDateUtc.Value).ToList();
        }

        var items = salesOrders.SelectMany(x => x.Items).ToList();
        var productIds = items.Select(x => x.ProductId).Distinct().ToList();

        var products = await dbContext.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var avgCostByProduct = await BuildAverageCostByProductAsync(cancellationToken);

        var result = items
            .GroupBy(x => x.ProductId)
            .Select(group =>
            {
                products.TryGetValue(group.Key, out var product);
                var quantity = group.Sum(x => x.Quantity);
                var revenue = group.Sum(x => x.Quantity * x.UnitPrice);
                var avgCost = avgCostByProduct.GetValueOrDefault(group.Key);
                var cost = quantity * avgCost;
                var profit = revenue - cost;
                var margin = revenue <= 0 ? 0 : (profit / revenue) * 100m;

                return new ProductProfitabilityDto(
                    group.Key,
                    product?.Code ?? string.Empty,
                    product?.Name ?? string.Empty,
                    Math.Round(quantity, 3),
                    Math.Round(revenue, 2),
                    Math.Round(cost, 2),
                    Math.Round(profit, 2),
                    Math.Round(margin, 2));
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return Ok(result);
    }

    [HttpGet("finance/profitability/customers")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerProfitabilityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerProfitabilityDto>>> GetCustomerProfitability(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.SalesOrders
            .AsNoTracking()
            .Where(x => x.Status == OrderStatus.Approved)
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        if (startDateUtc.HasValue)
        {
            orders = orders.Where(x => x.OrderDateUtc >= startDateUtc.Value).ToList();
        }

        if (endDateUtc.HasValue)
        {
            orders = orders.Where(x => x.OrderDateUtc <= endDateUtc.Value).ToList();
        }

        var cariMap = await dbContext.CariAccounts
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var avgCostByProduct = await BuildAverageCostByProductAsync(cancellationToken);

        var result = orders
            .GroupBy(x => x.CustomerCariAccountId)
            .Select(group =>
            {
                var revenue = group.Sum(order => order.Items.Sum(i => i.Quantity * i.UnitPrice));
                var cost = group.Sum(order => order.Items.Sum(i => i.Quantity * avgCostByProduct.GetValueOrDefault(i.ProductId)));
                var profit = revenue - cost;
                var margin = revenue <= 0 ? 0 : (profit / revenue) * 100m;

                cariMap.TryGetValue(group.Key, out var cari);

                return new CustomerProfitabilityDto(
                    group.Key,
                    cari?.Code ?? string.Empty,
                    cari?.Name ?? string.Empty,
                    Math.Round(revenue, 2),
                    Math.Round(cost, 2),
                    Math.Round(profit, 2),
                    Math.Round(margin, 2));
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return Ok(result);
    }

    [HttpGet("finance/profitability/branches")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchProfitabilityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchProfitabilityDto>>> GetBranchProfitability(
        [FromQuery] DateTime? startDateUtc,
        [FromQuery] DateTime? endDateUtc,
        CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.SalesOrders
            .AsNoTracking()
            .Where(x => x.Status == OrderStatus.Approved)
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        if (startDateUtc.HasValue)
        {
            orders = orders.Where(x => x.OrderDateUtc >= startDateUtc.Value).ToList();
        }

        if (endDateUtc.HasValue)
        {
            orders = orders.Where(x => x.OrderDateUtc <= endDateUtc.Value).ToList();
        }

        var warehouses = await dbContext.Warehouses
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var branchIds = warehouses.Values.Select(x => x.BranchId).Distinct().ToList();
        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(x => branchIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var avgCostByProduct = await BuildAverageCostByProductAsync(cancellationToken);

        var result = orders
            .GroupBy(order => warehouses.TryGetValue(order.WarehouseId, out var wh) ? wh.BranchId : Guid.Empty)
            .Where(group => group.Key != Guid.Empty)
            .Select(group =>
            {
                var revenue = group.Sum(order => order.Items.Sum(i => i.Quantity * i.UnitPrice));
                var cost = group.Sum(order => order.Items.Sum(i => i.Quantity * avgCostByProduct.GetValueOrDefault(i.ProductId)));
                var profit = revenue - cost;
                var margin = revenue <= 0 ? 0 : (profit / revenue) * 100m;

                branches.TryGetValue(group.Key, out var branch);

                return new BranchProfitabilityDto(
                    group.Key,
                    branch?.Code ?? string.Empty,
                    branch?.Name ?? string.Empty,
                    Math.Round(revenue, 2),
                    Math.Round(cost, 2),
                    Math.Round(profit, 2),
                    Math.Round(margin, 2));
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        return Ok(result);
    }

    private async Task<Dictionary<Guid, decimal>> BuildAverageCostByProductAsync(CancellationToken cancellationToken)
    {
        var inMovements = await dbContext.StockMovements
            .AsNoTracking()
            .Where(x => x.Type == StockMovementType.In)
            .ToListAsync(cancellationToken);

        return inMovements
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var totalQty = group.Sum(x => x.Quantity);
                    if (totalQty <= 0)
                    {
                        return 0m;
                    }

                    var totalAmount = group.Sum(x => x.Quantity * x.UnitPrice);
                    return totalAmount / totalQty;
                });
    }
}

