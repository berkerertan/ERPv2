using ERP.Domain.Common;
using ERP.Domain.Enums;

namespace ERP.Domain.Entities;

public sealed class CariAccount : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CariType Type { get; set; }
    public decimal RiskLimit { get; set; }
    public int MaturityDays { get; set; }
    public decimal CurrentBalance { get; set; }
}
