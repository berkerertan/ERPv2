namespace ERP.Application.Features.Warehouses.Queries.GetWarehouses;

public sealed record WarehouseDto(Guid Id, Guid BranchId, string Code, string Name);
