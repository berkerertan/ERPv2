namespace ERP.API.Contracts.Auth;

/* ─── 2FA (İki Faktörlü Kimlik Doğrulama) ─────────────────── */

public sealed record TwoFactorStatusResponse(bool IsEnabled, bool HasAuthenticator);

public sealed record TwoFactorSetupResponse(string SharedKey, string QrCodeUri);

public sealed class TwoFactorVerifyRequest
{
    public string Code { get; init; } = string.Empty;
}
