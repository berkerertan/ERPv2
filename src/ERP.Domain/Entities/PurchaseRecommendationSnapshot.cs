using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class PurchaseRecommendationSnapshot : TenantOwnedEntity
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public Guid? SupplierCariAccountId { get; set; }
    public string? SupplierName { get; set; }
    public int AnalysisDays { get; set; }
    public int CoverageDays { get; set; }
    public int MaxItems { get; set; }
    public bool CriticalOnly { get; set; }
    public int TotalItems { get; set; }
    public int CriticalItems { get; set; }
    public decimal TotalRecommendedQuantity { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public string SupplierGroupsJson { get; set; } = "[]";
    public string? CreatedByUserName { get; set; }
}
