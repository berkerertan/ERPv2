namespace ERP.API.Contracts.Auth;

/* ─── Bildirim Tercihleri ──────────────────────────────────── */

public sealed record NotificationPreferencesDto(
    bool EmailInvoice,
    bool EmailPayment,
    bool EmailReminder,
    bool EmailMarketing,
    bool PushEnabled,
    bool PushOrderStatus,
    bool PushStockAlert);

public sealed class UpdateNotificationPreferencesRequest
{
    public bool? EmailInvoice { get; init; }
    public bool? EmailPayment { get; init; }
    public bool? EmailReminder { get; init; }
    public bool? EmailMarketing { get; init; }
    public bool? PushEnabled { get; init; }
    public bool? PushOrderStatus { get; init; }
    public bool? PushStockAlert { get; init; }
}
