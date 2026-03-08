namespace ERP.API.Contracts.Warehouses;

public sealed record UpdateWarehouseRequest(Guid BranchId, string Code, string Name);
