namespace ERP.Application.Abstractions.Auditing;

public interface IBusinessActivityService
{
    Task LogAsync(BusinessActivityLogEntry entry, CancellationToken cancellationToken = default);
}
