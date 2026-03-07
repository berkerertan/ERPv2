using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Warehouse : BaseEntity
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
