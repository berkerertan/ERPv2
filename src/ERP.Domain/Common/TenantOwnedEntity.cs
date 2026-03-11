namespace ERP.Domain.Common;

public abstract class TenantOwnedEntity : BaseEntity, ITenantOwnedEntity
{
    public Guid TenantAccountId { get; set; }
}
