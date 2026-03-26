using ERP.Application.Abstractions.Notifications;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ERP.Infrastructure.Communication;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var smtp = options.Value;
        if (!smtp.Enabled)
        {
            return new EmailSendResult(false, true, "SMTP is disabled.");
        }

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            return new EmailSendResult(false, false, "SMTP host is not configured.");
        }

        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(smtp.FromEmail, smtp.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };
            mail.To.Add(message.To);

            using var client = new SmtpClient(smtp.Host, smtp.Port)
            {
                EnableSsl = smtp.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(smtp.UserName))
            {
                client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);
            }

            using var ctr = cancellationToken.Register(static state =>
            {
                if (state is SmtpClient smtpClient)
                {
                    smtpClient.SendAsyncCancel();
                }
            }, client);

            await client.SendMailAsync(mail, cancellationToken);
            return new EmailSendResult(true, false, "Email sent.");
        }
        catch (Exception ex)
        {
            return new EmailSendResult(false, false, ex.Message);
        }
    }
}
