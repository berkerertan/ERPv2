namespace ERP.Domain.Common;

public interface ITenantOwnedEntity
{
    Guid TenantAccountId { get; set; }
}
