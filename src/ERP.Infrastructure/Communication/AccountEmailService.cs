using ERP.Application.Abstractions.Notifications;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERP.Infrastructure.Communication;

public sealed class AccountEmailService(
    IEmailSender emailSender,
    IOptions<AccountEmailOptions> options,
    ErpDbContext dbContext,
    ILogger<AccountEmailService> logger) : IAccountEmailService
{
    public Task<EmailSendResult> SendVerificationEmailAsync(
        AppUser user,
        string verificationToken,
        CancellationToken cancellationToken = default) =>
        SendVerificationEmailInternalAsync(user, verificationToken, cancellationToken);

    private async Task<EmailSendResult> SendVerificationEmailInternalAsync(
        AppUser user,
        string verificationToken,
        CancellationToken cancellationToken)
    {
        var appOptions = options.Value;
        var verifyUrl = BuildVerificationUrl(appOptions, user.Email, verificationToken);
        var subject = $"{appOptions.ProductName} - E-posta Dogrulama";
        var body = BuildVerificationHtml(appOptions.ProductName, user.UserName, verifyUrl);
        var attemptedAt = DateTime.UtcNow;

        var sendResult = await emailSender.SendAsync(
            new EmailMessage(user.Email, subject, body, IsHtml: true),
            cancellationToken);

        var tenant = user.TenantAccountId.HasValue
            ? await dbContext.TenantAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == user.TenantAccountId.Value, cancellationToken)
            : null;

        var status = sendResult.IsSuccess
            ? "Sent"
            : sendResult.IsSkipped
                ? "Skipped"
                : "Failed";

        dbContext.PlatformEmailDispatchLogs.Add(new PlatformEmailDispatchLog
        {
            CampaignId = null,
            TenantAccountId = user.TenantAccountId,
            TenantCode = tenant?.Code,
            TenantName = tenant?.Name,
            TemplateKey = EmailTemplateKeys.AccountVerification,
            RecipientEmail = NormalizeEmail(user.Email),
            Subject = subject,
            Body = body,
            Status = status,
            ProviderMessage = sendResult.Message,
            AttemptedAtUtc = attemptedAt,
            SentAtUtc = sendResult.IsSuccess ? attemptedAt : null,
            TriggeredByUserId = user.Id,
            TriggeredByUserName = user.UserName
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Account verification email dispatch logged for user {UserId}. Status: {Status}",
            user.Id,
            status);

        return sendResult;
    }

    private static string BuildVerificationUrl(AccountEmailOptions options, string email, string token)
    {
        var baseUrl = (options.FrontendBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        var path = (options.VerifyEmailPath ?? "/auth/verify-email").Trim();
        if (!path.StartsWith("/", StringComparison.Ordinal))
        {
            path = "/" + path;
        }

        var encodedEmail = Uri.EscapeDataString(email ?? string.Empty);
        var encodedToken = Uri.EscapeDataString(token ?? string.Empty);
        return $"{baseUrl}{path}?email={encodedEmail}&token={encodedToken}";
    }

    private static string BuildVerificationHtml(string productName, string userName, string verifyUrl)
    {
        var safeProduct = string.IsNullOrWhiteSpace(productName) ? "StokNet" : productName;
        var safeName = string.IsNullOrWhiteSpace(userName) ? "Degerli kullanici" : userName;
        var safeUrl = verifyUrl ?? string.Empty;

        return $$"""
<!doctype html>
<html lang="tr">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>E-posta Dogrulama</title>
</head>
<body style="margin:0;padding:0;background:#f3f4f6;font-family:Segoe UI,Arial,sans-serif;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="padding:28px 12px;">
    <tr>
      <td align="center">
        <table role="presentation" width="640" cellspacing="0" cellpadding="0" style="width:100%;max-width:640px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e5e7eb;box-shadow:0 16px 40px rgba(15,23,42,.12);">
          <tr>
            <td style="padding:26px 30px;background:linear-gradient(135deg,#0f172a 0%,#1e293b 100%);">
              <div style="display:inline-block;padding:6px 12px;border-radius:999px;background:#f97316;color:#fff;font-weight:700;font-size:12px;letter-spacing:.08em;">HESAP DOGRULAMA</div>
              <h1 style="margin:14px 0 0 0;color:#fff;font-size:24px;line-height:1.3;">{{safeProduct}} hesabini etkinlestir</h1>
              <p style="margin:10px 0 0 0;color:#cbd5e1;font-size:14px;line-height:1.6;">Hizli bir adim kaldi. E-posta adresini dogrulayarak hesabinizi guvenli sekilde kullanmaya baslayin.</p>
            </td>
          </tr>
          <tr>
            <td style="padding:28px 30px 8px 30px;">
              <p style="margin:0 0 14px 0;color:#111827;font-size:16px;line-height:1.6;">Merhaba <strong>{{safeName}}</strong>,</p>
              <p style="margin:0;color:#4b5563;font-size:15px;line-height:1.7;">Asagidaki butona tiklayarak e-posta adresinizi dogrulayabilirsiniz. Bu baglanti guvenlik nedenleriyle sinirli sure gecerlidir.</p>
            </td>
          </tr>
          <tr>
            <td style="padding:20px 30px;">
              <a href="{{safeUrl}}" style="display:inline-block;padding:14px 24px;border-radius:12px;background:#f97316;color:#fff;text-decoration:none;font-size:15px;font-weight:700;">E-postami Dogrula</a>
            </td>
          </tr>
          <tr>
            <td style="padding:0 30px 22px 30px;">
              <p style="margin:0 0 10px 0;color:#6b7280;font-size:13px;line-height:1.6;">Buton calismazsa asagidaki baglantiyi tarayiciniza yapistirin:</p>
              <p style="margin:0;word-break:break-all;"><a href="{{safeUrl}}" style="color:#2563eb;font-size:13px;text-decoration:none;">{{safeUrl}}</a></p>
            </td>
          </tr>
          <tr>
            <td style="padding:18px 30px 26px 30px;background:#f8fafc;border-top:1px solid #e5e7eb;">
              <p style="margin:0;color:#64748b;font-size:12px;line-height:1.7;">Bu e-postayi siz talep etmediyseniz dikkate almayabilirsiniz.</p>
              <p style="margin:8px 0 0 0;color:#94a3b8;font-size:12px;">{{safeProduct}} Guvenlik Bildirimi</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
    }

    private static string NormalizeEmail(string email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}
