namespace ERP.API.Contracts.Warehouses;

public sealed record CreateWarehouseRequest(Guid BranchId, string Code, string Name);
