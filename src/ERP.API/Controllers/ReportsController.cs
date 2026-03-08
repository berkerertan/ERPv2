using ERP.Application.Features.Reports.Queries.GetCariAging;
using ERP.Application.Features.Reports.Queries.GetCariBalances;
using ERP.Application.Features.Reports.Queries.GetIncomeExpenseSummary;
using ERP.Application.Features.Reports.Queries.GetPurchaseSummary;
using ERP.Application.Features.Reports.Queries.GetSalesSummary;
using ERP.Application.Features.StockMovements.Queries.GetStockBalances;
using ERP.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("stock")]
    public async Task<IActionResult> GetStock(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStockBalancesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSales(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetSalesSummaryQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetPurchaseSummaryQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("cari-balances")]
    public async Task<IActionResult> GetCariBalances(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariBalancesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("cari-aging")]
    public async Task<IActionResult> GetCariAging(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetCariAgingQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("income-expense")]
    public async Task<IActionResult> GetIncomeExpense(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetIncomeExpenseSummaryQuery(), cancellationToken);
        return Ok(response);
    }
}
