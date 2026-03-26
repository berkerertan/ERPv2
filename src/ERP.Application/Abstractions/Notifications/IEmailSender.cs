namespace ERP.Application.Abstractions.Notifications;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(
    string To,
    string Subject,
    string Body,
    bool IsHtml = true);

public sealed record EmailSendResult(
    bool IsSuccess,
    bool IsSkipped,
    string Message);
