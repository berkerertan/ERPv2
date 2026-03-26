namespace ERP.API.Contracts.Auth;

/* ─── Aktif Oturumlar ──────────────────────────────────────── */

public sealed record ActiveSessionDto(
    Guid Id,
    string DeviceName,
    string IpAddress,
    string? Location,
    DateTime LastActiveUtc,
    bool IsCurrent);
