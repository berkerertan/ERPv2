using ERP.Domain.Common;

namespace ERP.Domain.Entities;

public sealed class Product : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "EA";
    public string Category { get; set; } = string.Empty;
}
