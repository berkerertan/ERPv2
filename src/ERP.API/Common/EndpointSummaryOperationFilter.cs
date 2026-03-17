using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ERP.API.Common;

public sealed class EndpointSummaryOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.IsNullOrWhiteSpace(operation.Summary))
        {
            return;
        }

        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? "GET";
        var relativePath = context.ApiDescription.RelativePath ?? string.Empty;
        var normalizedPath = "/" + relativePath.Split('?')[0].Trim('/');

        operation.Summary = BuildSummary(method, normalizedPath);
        operation.Description ??= $"Method: {method}. Path: {normalizedPath}.";
    }

    private static string BuildSummary(string method, string path)
    {
        var p = path.ToLowerInvariant();

        if (p == "/api/auth/login")
        {
            return "Kullanici girisi yapar ve JWT token dondurur.";
        }

        if (p == "/api/auth/register")
        {
            return "Yeni kullanici hesabi olusturur ve kullaniciyi secilen kademeye atar.";
        }

        if (p == "/api/auth/register-saas")
        {
            return "Abone kaydi olusturur, plan secimine gore tenant ve kademe rolunu atar.";
        }

        if (p == "/api/auth/bootstrap-admin")
        {
            return "Sistemde tek platform admin kullanicisini olusturur.";
        }

        if (p == "/api/auth/subscription-plans")
        {
            return "Abonelik kademelerini, fiyatlarini ve atanacak rol bilgisini listeler.";
        }

        if (p == "/api/auth/refresh")
        {
            return "Refresh token ile yeni access token uretir.";
        }

        if (p == "/api/auth/me")
        {
            return "Giris yapan kullanicinin aktif oturum bilgilerini getirir.";
        }

        if (p == "/api/auth/logout")
        {
            return "Refresh token i pasife alarak oturumu kapatir.";
        }

        if (p.Contains("/platform-admin/audit-logs/summary"))
        {
            return "Admin paneli icin audit log ozet metriklerini getirir.";
        }

        if (p.Contains("/platform-admin/audit-logs"))
        {
            return "Admin paneli icin sistem audit log kayitlarini listeler veya detayini verir.";
        }

        if (p.Contains("/api/activity-logs/me/summary"))
        {
            return "Giris yapan kullanicinin kendi islem gecmisi ozet metriklerini getirir.";
        }

        if (p.Contains("/api/activity-logs/me"))
        {
            return "Giris yapan kullanicinin kendi islem gecmisi kayitlarini listeler.";
        }

        if (p.Contains("/suggest"))
        {
            return "Arama kutusu icin hizli onerileri listeler.";
        }

        if (p.Contains("/api/cari-accounts/buyers/import-excel"))
        {
            return "Birden fazla alici Excel dosyasini yukler; alici adini dosya isminden cikarip borc kalemlerini ice aktarir.";
        }

        if (p.Contains("/import-excel"))
        {
            return "Excel dosyasindan toplu veri ice aktarir.";
        }

        if (p.Contains("/scan"))
        {
            return "Barkod ile urun bilgisini getirir.";
        }

        if (p.Contains("/api/products/bulk-price-update"))
        {
            return "Secili urunlerin satis fiyatlarini toplu olarak gunceller.";
        }

        if (p.Contains("/api/products/bulk-stock-update"))
        {
            return "Secili urunler icin toplu stok artisi/azalisi hareketleri olusturur.";
        }

        if (p.Contains("/api/platform-admin/announcements"))
        {
            return "Platform admin duyurulari olusturur, gunceller ve yayin durumunu yonetir.";
        }

        if (p.Contains("/api/announcements"))
        {
            return "Yayinda olan duyurulari tum kullanicilar icin listeler veya detayini getirir.";
        }

        if (p.Contains("/quick-sales"))
        {
            return "Hizli satis islemi olusturur.";
        }

        if (p.Contains("/transfer"))
        {
            return "Depolar arasi stok transferi yapar.";
        }

        if (p.Contains("/critical-alerts"))
        {
            return "Kritik stok seviyesindeki urunleri listeler.";
        }

        if (p.Contains("/cari-accounts") && p.Contains("/details"))
        {
            return "Secilen cari hesabin detay ve ozet bilgilerini getirir.";
        }

        if (p.Contains("/cari-accounts") && p.Contains("/debt-items"))
        {
            if (method == "GET" && p.Contains("{debtitemid}"))
            {
                return "Secilen veresiye kaleminin detayini getirir.";
            }

            if (method == "GET")
            {
                return "Cari hesaba ait veresiye kalemlerini listeler.";
            }

            if (method == "POST")
            {
                return "Cari hesaba yeni veresiye kalemi ekler.";
            }

            if (method == "PUT")
            {
                return "Veresiye kalemini gunceller.";
            }

            if (method == "DELETE")
            {
                return "Veresiye kalemini soft delete ile pasife alir.";
            }
        }

        if (p.Contains("/cari-accounts/suppliers"))
        {
            return "Yalnizca tedarikci carileri listeler.";
        }

        if (p.Contains("/cari-accounts/buyers"))
        {
            return "Yalnizca alici carileri listeler.";
        }

        if (p.Contains("/api/accounting/chart-of-accounts"))
        {
            return "Muhasebe hesap plani kayitlarini yonetir.";
        }

        if (p.Contains("/api/accounting/journal-entries") && p.Contains("/post"))
        {
            return "Yevmiye fisini kontrol edip posted durumuna alir.";
        }

        if (p.Contains("/api/accounting/journal-entries") && p.Contains("/reverse"))
        {
            return "Yevmiye fisinin ters kaydini olusturur.";
        }

        if (p.Contains("/api/accounting/journal-entries"))
        {
            return "Yevmiye fislerini olusturur, listeler ve gunceller.";
        }

        if (p.Contains("/api/accounting/collections-payments"))
        {
            return "Cariye bagli tahsilat/odeme islemini kasa veya banka ile birlikte kaydeder.";
        }

        if (p.Contains("/api/accounting/cash-accounts") || p.Contains("/api/accounting/cash-transactions"))
        {
            return "Kasa hesaplari ve kasa hareketlerini yonetir.";
        }

        if (p.Contains("/api/accounting/bank-accounts") || p.Contains("/api/accounting/bank-transactions"))
        {
            return "Banka hesaplari ve banka hareketlerini yonetir.";
        }

        if (p.Contains("/api/invoices/from-sales-order"))
        {
            return "Onayli satis siparisinden satis faturasi olusturur.";
        }

        if (p.Contains("/api/invoices/from-purchase-order"))
        {
            return "Onayli satin alma siparisinden alis faturasi olusturur.";
        }

        if (p.Contains("/api/invoices/e-fatura"))
        {
            return "E-Fatura tipindeki faturalarin liste ekranini getirir.";
        }

        if (p.Contains("/api/invoices/e-arsiv"))
        {
            return "E-Arsiv tipindeki faturalarin liste ekranini getirir.";
        }

        if (p.Contains("/api/invoices/") && p.Contains("/detail"))
        {
            return "Fatura baslik bilgisi ile urun kalemlerini tek response icinde dondurur.";
        }

        if (p.Contains("/api/invoices/") && p.Contains("/preview-html"))
        {
            return "Faturanin yazdirilabilir HTML onizlemesini dondurur.";
        }

        if (p.Contains("/reports/finance/profitability"))
        {
            return "Karlilik raporlarini urun/musteri/sube bazinda getirir.";
        }

        if (p.Contains("/reports/finance/cash-flow-forecast"))
        {
            return "Vadeye gore beklenen nakit giris-cikis tahminini listeler.";
        }

        if (p.Contains("/reports/finance/due-list"))
        {
            return "Vade listesi ve gecikme gunu bilgilerini dondurur.";
        }

        if (p.Contains("/reports"))
        {
            return "Rapor verisini listeler veya ozet cikti dondurur.";
        }

        var hasId = p.Contains("/{id}") || p.Contains("/{debtitemid}") || p.Contains("/{cariaccountid}");

        return method switch
        {
            "GET" when hasId => "Tek kaydin detayini getirir.",
            "GET" => "Kayitlari listeler.",
            "POST" => "Yeni kayit olusturur.",
            "PUT" => "Kaydi gunceller.",
            "DELETE" => "Kaydi soft delete ile pasife alir.",
            _ => "Endpoint islemini calistirir."
        };
    }
}



