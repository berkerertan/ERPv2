using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Branch : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
