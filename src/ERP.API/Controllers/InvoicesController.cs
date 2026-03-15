using ERP.API.Common;
using ERP.API.Contracts.Invoices;
using ERP.Domain.Entities;
using ERP.Domain.Constants;
using ERP.Domain.Enums;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/invoices")]
[RequirePolicy("TierUserOrAdmin")]
[RequireSubscriptionFeature(SubscriptionFeatures.Invoices)]
public sealed class InvoicesController(ErpDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> GetAll(
        [FromQuery] InvoiceType? invoiceType,
        [FromQuery] InvoiceCategory? invoiceCategory,
        [FromQuery] InvoiceStatus? status,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(dbContext.Invoices.AsNoTracking(), invoiceType, invoiceCategory, status);

        var invoices = await query
            .OrderByDescending(x => x.IssueDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => MapInvoice(x))
            .ToListAsync(cancellationToken);

        return Ok(invoices);
    }

    [HttpGet("e-fatura")]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvoiceListItemDto>>> GetEFaturaList(
        [FromQuery] InvoiceCategory? invoiceCategory,
        [FromQuery] InvoiceStatus? status,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(dbContext.Invoices.AsNoTracking(), InvoiceType.EFatura, invoiceCategory, status);
        var invoices = await query
            .OrderByDescending(x => x.IssueDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(invoices.Select(MapListItem).ToList());
    }

    [HttpGet("e-arsiv")]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvoiceListItemDto>>> GetEArsivList(
        [FromQuery] InvoiceCategory? invoiceCategory,
        [FromQuery] InvoiceStatus? status,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(dbContext.Invoices.AsNoTracking(), InvoiceType.EArsiv, invoiceCategory, status);
        var invoices = await query
            .OrderByDescending(x => x.IssueDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(invoices.Select(MapListItem).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(MapInvoice(invoice));
    }

    [HttpGet("{id:guid}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvoiceItemDto>>> GetItems(Guid id, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Invoices.AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var items = await dbContext.InvoiceItems
            .AsNoTracking()
            .Where(x => x.InvoiceId == id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => MapItem(x))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(InvoiceSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var invoices = await dbContext.Invoices.AsNoTracking().ToListAsync(cancellationToken);

        var summary = new InvoiceSummaryDto(
            invoices.Count,
            invoices.Count(x => x.Status == InvoiceStatus.Draft),
            invoices.Count(x => x.Status == InvoiceStatus.Sent),
            invoices.Count(x => x.Status == InvoiceStatus.Approved),
            invoices.Count(x => x.Status == InvoiceStatus.Rejected),
            invoices.Sum(x => x.GrandTotal));

        return Ok(summary);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest("Invoice must contain at least one item.");
        }

        var cari = await dbContext.CariAccounts.FirstOrDefaultAsync(x => x.Id == request.CariAccountId, cancellationToken);
        if (cari is null)
        {
            return BadRequest("Cari account not found.");
        }

        var products = await dbContext.Products
            .Where(x => request.Items.Select(i => i.ProductId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != request.Items.Count)
        {
            return BadRequest("One or more products were not found.");
        }

        var invoice = new Invoice
        {
            InvoiceNumber = NormalizeInvoiceNumber(request.InvoiceNumber),
            InvoiceType = request.InvoiceType,
            InvoiceCategory = request.InvoiceCategory,
            Status = InvoiceStatus.Draft,
            CariAccountId = cari.Id,
            CariAccountName = cari.Name,
            TaxNumber = NormalizeTaxNumber(request.TaxNumber),
            IssueDateUtc = request.IssueDate,
            DueDateUtc = request.DueDate,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant(),
            Notes = request.Notes
        };

        invoice.Items = BuildItems(invoice.Id, request.Items, products, out var subtotal, out var taxTotal, out var discountTotal, out var grandTotal);
        invoice.Subtotal = subtotal;
        invoice.TaxTotal = taxTotal;
        invoice.DiscountTotal = discountTotal;
        invoice.GrandTotal = grandTotal;

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/invoices/{invoice.Id}", MapInvoice(invoice));
    }


    [HttpPost("from-sales-order/{salesOrderId:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InvoiceDto>> CreateFromSalesOrder(
        Guid salesOrderId,
        [FromBody] CreateInvoiceFromOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.SalesOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);

        if (order is null)
        {
            return NotFound("Sales order not found.");
        }

        if (order.Status != OrderStatus.Approved)
        {
            return BadRequest("Only approved sales orders can be invoiced.");
        }

        var alreadyExists = await dbContext.Invoices.AnyAsync(x => x.SalesOrderId == salesOrderId, cancellationToken);
        if (alreadyExists)
        {
            return Conflict("An invoice for this sales order already exists.");
        }

        var cari = await dbContext.CariAccounts.FirstOrDefaultAsync(x => x.Id == order.CustomerCariAccountId, cancellationToken);
        if (cari is null)
        {
            return BadRequest("Customer cari account not found.");
        }

        var productIds = order.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return BadRequest("One or more products could not be resolved from sales order.");
        }

        var invoice = new Invoice
        {
            InvoiceNumber = NormalizeInvoiceNumber(request.InvoiceNumber),
            InvoiceType = request.InvoiceType,
            InvoiceCategory = InvoiceCategory.Satis,
            Status = InvoiceStatus.Draft,
            SalesOrderId = order.Id,
            CariAccountId = cari.Id,
            CariAccountName = cari.Name,
            TaxNumber = NormalizeTaxNumber(request.TaxNumber),
            IssueDateUtc = request.IssueDate ?? DateTime.UtcNow,
            DueDateUtc = request.DueDate,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant(),
            Notes = request.Notes
        };

        invoice.Items = BuildItemsFromOrder(invoice.Id, order.Items, products, x => x.ProductId, x => x.Quantity, x => x.UnitPrice, out var subtotal, out var taxTotal, out var discountTotal, out var grandTotal);
        invoice.Subtotal = subtotal;
        invoice.TaxTotal = taxTotal;
        invoice.DiscountTotal = discountTotal;
        invoice.GrandTotal = grandTotal;

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/invoices/{invoice.Id}", MapInvoice(invoice));
    }

    [HttpPost("from-purchase-order/{purchaseOrderId:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InvoiceDto>> CreateFromPurchaseOrder(
        Guid purchaseOrderId,
        [FromBody] CreateInvoiceFromOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == purchaseOrderId, cancellationToken);

        if (order is null)
        {
            return NotFound("Purchase order not found.");
        }

        if (order.Status != OrderStatus.Approved)
        {
            return BadRequest("Only approved purchase orders can be invoiced.");
        }

        var alreadyExists = await dbContext.Invoices.AnyAsync(x => x.PurchaseOrderId == purchaseOrderId, cancellationToken);
        if (alreadyExists)
        {
            return Conflict("An invoice for this purchase order already exists.");
        }

        var cari = await dbContext.CariAccounts.FirstOrDefaultAsync(x => x.Id == order.SupplierCariAccountId, cancellationToken);
        if (cari is null)
        {
            return BadRequest("Supplier cari account not found.");
        }

        var productIds = order.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return BadRequest("One or more products could not be resolved from purchase order.");
        }

        var invoice = new Invoice
        {
            InvoiceNumber = NormalizeInvoiceNumber(request.InvoiceNumber),
            InvoiceType = request.InvoiceType,
            InvoiceCategory = InvoiceCategory.Alis,
            Status = InvoiceStatus.Draft,
            PurchaseOrderId = order.Id,
            CariAccountId = cari.Id,
            CariAccountName = cari.Name,
            TaxNumber = NormalizeTaxNumber(request.TaxNumber),
            IssueDateUtc = request.IssueDate ?? DateTime.UtcNow,
            DueDateUtc = request.DueDate,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant(),
            Notes = request.Notes
        };

        invoice.Items = BuildItemsFromOrder(invoice.Id, order.Items, products, x => x.ProductId, x => x.Quantity, x => x.UnitPrice, out var subtotal, out var taxTotal, out var discountTotal, out var grandTotal);
        invoice.Subtotal = subtotal;
        invoice.TaxTotal = taxTotal;
        invoice.DiscountTotal = discountTotal;
        invoice.GrandTotal = grandTotal;

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Created($"/api/invoices/{invoice.Id}", MapInvoice(invoice));
    }
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (invoice is null)
        {
            return NotFound();
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return BadRequest("Only draft invoices can be updated.");
        }

        var products = await dbContext.Products
            .Where(x => request.Items.Select(i => i.ProductId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != request.Items.Count)
        {
            return BadRequest("One or more products were not found.");
        }

        invoice.InvoiceType = request.InvoiceType;
        invoice.InvoiceCategory = request.InvoiceCategory;
        invoice.InvoiceNumber = NormalizeInvoiceNumber(request.InvoiceNumber);
        invoice.TaxNumber = NormalizeTaxNumber(request.TaxNumber);
        invoice.IssueDateUtc = request.IssueDate;
        invoice.DueDateUtc = request.DueDate;
        invoice.Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.Trim().ToUpperInvariant();
        invoice.Notes = request.Notes;
        invoice.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var oldItem in invoice.Items)
        {
            oldItem.MarkAsDeleted();
        }

        invoice.Items = BuildItems(invoice.Id, request.Items, products, out var subtotal, out var taxTotal, out var discountTotal, out var grandTotal);
        invoice.Subtotal = subtotal;
        invoice.TaxTotal = taxTotal;
        invoice.DiscountTotal = discountTotal;
        invoice.GrandTotal = grandTotal;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (invoice is null)
        {
            return NotFound();
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return BadRequest("Only draft invoices can be deleted.");
        }

        invoice.MarkAsDeleted();
        foreach (var item in invoice.Items)
        {
            item.MarkAsDeleted();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceDto>> Send(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        if (invoice.Status != InvoiceStatus.Draft)
        {
            return BadRequest("Only draft invoices can be sent.");
        }

        var stamp = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.Sent;
        invoice.InvoiceNumber = string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
            ? $"INV{stamp:yyyyMMdd}-{stamp:HHmmss}"
            : invoice.InvoiceNumber;
        invoice.Uuid ??= Guid.NewGuid().ToString("D").ToUpperInvariant();
        invoice.Ettn ??= Guid.NewGuid().ToString("D").ToUpperInvariant();
        invoice.GibResponseCode = "200";
        invoice.GibResponseDescription = "Sent to GIB queue";
        invoice.UpdatedAtUtc = stamp;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(MapInvoice(invoice));
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvoiceDto>> Cancel(Guid id, [FromBody] CancelInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        invoice.Status = InvoiceStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            invoice.Notes = string.IsNullOrWhiteSpace(invoice.Notes)
                ? $"Cancel reason: {request.Reason}"
                : $"{invoice.Notes}\nCancel reason: {request.Reason}";
        }

        invoice.GibResponseCode = "CANCELLED";
        invoice.GibResponseDescription = request.Reason;
        invoice.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(MapInvoice(invoice));
    }

    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var content = $"Invoice: {invoice.InvoiceNumber}\nCari: {invoice.CariAccountName}\nTotal: {invoice.GrandTotal:F2} {invoice.Currency}";
        var bytes = Encoding.UTF8.GetBytes(content);

        return File(bytes, "application/pdf", $"{GetSafeInvoiceNo(invoice)}.pdf");
    }

    [HttpGet("{id:guid}/xml")]
    [Produces("application/xml")]
    public async Task<IActionResult> GetXml(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (invoice is null)
        {
            return NotFound();
        }

        var xml = $"<Invoice><Id>{invoice.Id}</Id><InvoiceNumber>{invoice.InvoiceNumber}</InvoiceNumber><Status>{invoice.Status}</Status><GrandTotal>{invoice.GrandTotal:F2}</GrandTotal><Currency>{invoice.Currency}</Currency></Invoice>";
        var bytes = Encoding.UTF8.GetBytes(xml);

        return File(bytes, "application/xml", $"{GetSafeInvoiceNo(invoice)}.xml");
    }

    private static string GetSafeInvoiceNo(Invoice invoice)
        => string.IsNullOrWhiteSpace(invoice.InvoiceNumber) ? invoice.Id.ToString("N") : invoice.InvoiceNumber;

    private static List<InvoiceItem> BuildItems(
        Guid invoiceId,
        IEnumerable<CreateInvoiceItemRequest> items,
        IReadOnlyDictionary<Guid, Product> products,
        out decimal subtotal,
        out decimal taxTotal,
        out decimal discountTotal,
        out decimal grandTotal)
    {
        var createdItems = new List<InvoiceItem>();
        subtotal = 0m;
        taxTotal = 0m;
        discountTotal = 0m;
        grandTotal = 0m;

        foreach (var item in items)
        {
            var product = products[item.ProductId];
            var gross = item.Quantity * item.UnitPrice;
            var discountAmount = Math.Round(gross * (item.DiscountRate / 100m), 2);
            var net = gross - discountAmount;
            var taxAmount = Math.Round(net * (item.TaxRate / 100m), 2);
            var lineTotal = net + taxAmount;

            subtotal += gross;
            taxTotal += taxAmount;
            discountTotal += discountAmount;
            grandTotal += lineTotal;

            createdItems.Add(new InvoiceItem
            {
                InvoiceId = invoiceId,
                ProductId = item.ProductId,
                ProductName = product.Name,
                Barcode = product.BarcodeEan13 ?? string.Empty,
                Quantity = item.Quantity,
                Unit = product.Unit,
                UnitPrice = item.UnitPrice,
                DiscountRate = item.DiscountRate,
                DiscountAmount = discountAmount,
                TaxRate = item.TaxRate,
                TaxAmount = taxAmount,
                LineTotal = lineTotal
            });
        }

        subtotal = Math.Round(subtotal, 2);
        taxTotal = Math.Round(taxTotal, 2);
        discountTotal = Math.Round(discountTotal, 2);
        grandTotal = Math.Round(grandTotal, 2);

        return createdItems;
    }


    private static List<InvoiceItem> BuildItemsFromOrder<TItem>(
        Guid invoiceId,
        IEnumerable<TItem> orderItems,
        IReadOnlyDictionary<Guid, Product> products,
        Func<TItem, Guid> productIdSelector,
        Func<TItem, decimal> quantitySelector,
        Func<TItem, decimal> unitPriceSelector,
        out decimal subtotal,
        out decimal taxTotal,
        out decimal discountTotal,
        out decimal grandTotal)
    {
        var createdItems = new List<InvoiceItem>();
        subtotal = 0m;
        taxTotal = 0m;
        discountTotal = 0m;
        grandTotal = 0m;

        foreach (var item in orderItems)
        {
            var productId = productIdSelector(item);
            var quantity = quantitySelector(item);
            var unitPrice = unitPriceSelector(item);

            var product = products[productId];
            var gross = quantity * unitPrice;
            var lineTotal = Math.Round(gross, 2);

            subtotal += lineTotal;
            grandTotal += lineTotal;

            createdItems.Add(new InvoiceItem
            {
                InvoiceId = invoiceId,
                ProductId = productId,
                ProductName = product.Name,
                Barcode = product.BarcodeEan13 ?? string.Empty,
                Quantity = quantity,
                Unit = product.Unit,
                UnitPrice = unitPrice,
                DiscountRate = 0m,
                DiscountAmount = 0m,
                TaxRate = 0m,
                TaxAmount = 0m,
                LineTotal = lineTotal
            });
        }

        subtotal = Math.Round(subtotal, 2);
        taxTotal = 0m;
        discountTotal = 0m;
        grandTotal = Math.Round(grandTotal, 2);

        return createdItems;
    }

    private static IQueryable<Invoice> ApplyFilters(
        IQueryable<Invoice> query,
        InvoiceType? invoiceType,
        InvoiceCategory? invoiceCategory,
        InvoiceStatus? status)
    {
        if (invoiceType.HasValue)
        {
            query = query.Where(x => x.InvoiceType == invoiceType.Value);
        }

        if (invoiceCategory.HasValue)
        {
            query = query.Where(x => x.InvoiceCategory == invoiceCategory.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return query;
    }

    private static InvoiceListItemDto MapListItem(Invoice invoice)
    {
        var isSupplierInvoice = invoice.InvoiceCategory == InvoiceCategory.Alis;

        return new InvoiceListItemDto(
            invoice.Id,
            string.IsNullOrWhiteSpace(invoice.InvoiceNumber) ? "-" : invoice.InvoiceNumber,
            invoice.InvoiceType,
            invoice.InvoiceCategory,
            isSupplierInvoice ? null : invoice.CariAccountId,
            isSupplierInvoice ? null : invoice.CariAccountName,
            isSupplierInvoice ? invoice.CariAccountId : null,
            isSupplierInvoice ? invoice.CariAccountName : null,
            invoice.TaxNumber,
            invoice.GrandTotal,
            invoice.TaxTotal,
            invoice.Status,
            MapStatusText(invoice.Status),
            invoice.IssueDateUtc);
    }

    private static string MapStatusText(InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Approved => "Onaylandi",
            InvoiceStatus.Rejected => "Red",
            InvoiceStatus.Cancelled => "Red",
            InvoiceStatus.Draft => "Bekliyor",
            InvoiceStatus.Sent => "Bekliyor",
            _ => "Bekliyor"
        };
    }

    private static string NormalizeInvoiceNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= 40 ? normalized : normalized[..40];
    }

    private static string NormalizeTaxNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= 20 ? normalized : normalized[..20];
    }

    private static InvoiceDto MapInvoice(Invoice invoice)
        => new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceType,
            invoice.InvoiceCategory,
            invoice.Status,
            invoice.CariAccountId,
            invoice.CariAccountName,
            invoice.TaxNumber,
            invoice.IssueDateUtc,
            invoice.DueDateUtc,
            invoice.Subtotal,
            invoice.TaxTotal,
            invoice.DiscountTotal,
            invoice.GrandTotal,
            invoice.Currency,
            invoice.Uuid,
            invoice.Ettn,
            invoice.GibResponseCode,
            invoice.GibResponseDescription,
            invoice.Notes,
            invoice.CreatedAtUtc,
            invoice.SalesOrderId,
            invoice.PurchaseOrderId);
    private static InvoiceItemDto MapItem(InvoiceItem item)
        => new(
            item.Id,
            item.InvoiceId,
            item.ProductId,
            item.ProductName,
            item.Barcode,
            item.Quantity,
            item.Unit,
            item.UnitPrice,
            item.DiscountRate,
            item.DiscountAmount,
            item.TaxRate,
            item.TaxAmount,
            item.LineTotal);
}






