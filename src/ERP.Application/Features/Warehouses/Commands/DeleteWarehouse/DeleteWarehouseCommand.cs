using MediatR;

namespace ERP.Application.Features.Warehouses.Commands.DeleteWarehouse;

public sealed record DeleteWarehouseCommand(Guid WarehouseId) : IRequest;
