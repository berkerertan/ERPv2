using ERP.Domain.Entities;

namespace ERP.Application.Abstractions.Notifications;

public interface IAccountEmailService
{
    Task<EmailSendResult> SendVerificationEmailAsync(
        AppUser user,
        string verificationToken,
        CancellationToken cancellationToken = default);
}
