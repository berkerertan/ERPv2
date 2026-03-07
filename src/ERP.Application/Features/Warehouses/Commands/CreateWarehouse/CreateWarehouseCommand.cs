using MediatR;

namespace ERP.Application.Features.Warehouses.Commands.CreateWarehouse;

public sealed record CreateWarehouseCommand(Guid BranchId, string Code, string Name) : IRequest<Guid>;
