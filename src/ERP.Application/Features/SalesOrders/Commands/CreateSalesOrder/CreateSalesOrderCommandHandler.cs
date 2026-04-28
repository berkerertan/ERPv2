using ERP.Application.Abstractions.Notifications;
using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.CreateSalesOrder;

public sealed class CreateSalesOrderCommandHandler(
    ISalesOrderRepository salesOrderRepository,
    ICariAccountRepository cariAccountRepository,
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository,
    IUserNotificationService userNotificationService)
    : IRequestHandler<CreateSalesOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        if (await salesOrderRepository.GetByOrderNoAsync(request.OrderNo, cancellationToken) is not null)
        {
            throw new ConflictException("Sales order number already exists.");
        }

        var buyerBch = await cariAccountRepository.GetByIdAsync(request.CustomerCariAccountId, cancellationToken)
            ?? throw new NotFoundException("Buyer/BCH cari account not found.");

        if (buyerBch.Type == CariType.Supplier)
        {
            throw new ConflictException("Selected cari account is not a buyer/BCH account.");
        }

        if (await warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken) is null)
        {
            throw new NotFoundException("Warehouse not found.");
        }

        foreach (var item in request.Items)
        {
            if (await productRepository.GetByIdAsync(item.ProductId, cancellationToken) is null)
            {
                throw new NotFoundException($"Product not found: {item.ProductId}");
            }
        }

        var order = new SalesOrder
        {
            OrderNo = request.OrderNo,
            CustomerCariAccountId = request.CustomerCariAccountId,
            WarehouseId = request.WarehouseId,
            Status = OrderStatus.Draft,
            OrderDateUtc = DateTime.UtcNow,
            Items = request.Items.Select(item => new SalesOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        await salesOrderRepository.AddAsync(order, cancellationToken);

        var total = order.Items.Sum(item => item.Quantity * item.UnitPrice);
        await userNotificationService.PublishAsync(
            "warning",
            "Satis siparisi onay bekliyor",
            $"{order.OrderNo} numarali siparis {buyerBch.Name} icin olusturuldu. Toplam: {total:N2} TRY.",
            "/sales-orders",
            cancellationToken);

        return order.Id;
    }
}
