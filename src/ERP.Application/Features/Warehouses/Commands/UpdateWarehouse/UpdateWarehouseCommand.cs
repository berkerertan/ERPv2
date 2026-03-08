using MediatR;

namespace ERP.Application.Features.Warehouses.Commands.UpdateWarehouse;

public sealed record UpdateWarehouseCommand(Guid WarehouseId, Guid BranchId, string Code, string Name) : IRequest;
