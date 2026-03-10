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
            return "Yeni kullanici hesabi olusturur.";
        }

        if (p == "/api/auth/bootstrap-admin")
        {
            return "Sistemde ilk admin kullanicisini olusturur.";
        }

        if (p.Contains("/suggest"))
        {
            return "Arama kutusu icin hizli onerileri listeler.";
        }

        if (p.Contains("/import-excel"))
        {
            return "Excel dosyasindan toplu veri ice aktarir.";
        }

        if (p.Contains("/scan"))
        {
            return "Barkod ile urun bilgisini getirir.";
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
