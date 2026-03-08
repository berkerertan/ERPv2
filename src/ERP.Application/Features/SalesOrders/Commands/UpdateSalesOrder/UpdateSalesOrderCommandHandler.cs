using ERP.Application.Abstractions.Persistence;
using ERP.Application.Common.Exceptions;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;

namespace ERP.Application.Features.SalesOrders.Commands.UpdateSalesOrder;

public sealed class UpdateSalesOrderCommandHandler(
    ISalesOrderRepository salesOrderRepository,
    ICariAccountRepository cariAccountRepository,
    IWarehouseRepository warehouseRepository,
    IProductRepository productRepository)
    : IRequestHandler<UpdateSalesOrderCommand>
{
    public async Task Handle(UpdateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await salesOrderRepository.GetWithItemsAsync(request.SalesOrderId, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        if (order.Status != OrderStatus.Draft)
        {
            throw new ConflictException("Only draft sales orders can be updated.");
        }

        var existingWithOrderNo = await salesOrderRepository.GetByOrderNoAsync(request.OrderNo, cancellationToken);
        if (existingWithOrderNo is not null && existingWithOrderNo.Id != order.Id)
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

        order.OrderNo = request.OrderNo;
        order.CustomerCariAccountId = request.CustomerCariAccountId;
        order.WarehouseId = request.WarehouseId;

        order.Items.Clear();
        foreach (var item in request.Items)
        {
            order.Items.Add(new SalesOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        await salesOrderRepository.UpdateAsync(order, cancellationToken);
    }
}
