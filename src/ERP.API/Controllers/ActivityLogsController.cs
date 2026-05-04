using ClosedXML.Excel;
using ERP.API.Common;
using ERP.API.Contracts.ActivityLogs;
using ERP.Application.Abstractions.Security;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/activity-logs")]
[RequirePolicy("TierUserOrAdmin")]
public sealed class ActivityLogsController(
    ErpDbContext dbContext,
    ICurrentTenantService currentTenantService) : ControllerBase
{
    [HttpGet("me/summary")]
    [ProducesResponseType(typeof(MyActivityLogSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MyActivityLogSummaryDto>> GetMySummary(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        [FromQuery] string? module = null,
        [FromQuery] string? httpMethod = null,
        [FromQuery] bool businessOnly = false,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var logs = await BuildMyLogQuery(userId, fromUtc, toUtc, onlyErrors, module, httpMethod, businessOnly, search)
            .ToListAsync(cancellationToken);

        var summary = new MyActivityLogSummaryDto(
            logs.Count,
            logs.Count(x => x.StatusCode >= 400),
            logs.Count(x => x.OccurredAtUtc >= DateTime.UtcNow.Date),
            logs.Count == 0 ? 0d : Math.Round(logs.Average(x => x.DurationMs), 2),
            logs.Count == 0 ? null : logs.Max(x => x.OccurredAtUtc));

        return Ok(summary);
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<MyActivityLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyActivityLogDto>>> GetMyLogs(
        [FromQuery] int? statusCode,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        [FromQuery] string? module = null,
        [FromQuery] string? httpMethod = null,
        [FromQuery] bool businessOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var query = BuildMyLogQuery(userId, fromUtc, toUtc, onlyErrors, module, httpMethod, businessOnly, search);

        if (statusCode.HasValue)
        {
            query = query.Where(x => x.StatusCode == statusCode.Value);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 500);

        var result = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(ToDtoExpression())
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("me/export")]
    public async Task<IActionResult> ExportMyLogsCsv(
        [FromQuery] int? statusCode,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        [FromQuery] string? module = null,
        [FromQuery] string? httpMethod = null,
        [FromQuery] bool businessOnly = false,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetExportLogs(userIdRequired: true, statusCode, fromUtc, toUtc, onlyErrors, module, httpMethod, businessOnly, search, cancellationToken);
        if (logs is null)
        {
            return Unauthorized();
        }

        var csv = BuildCsv(logs);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var fileName = BuildSafeFileName($"stoknet-aktivite-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("me/export/excel")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> ExportMyLogsExcel(
        [FromQuery] int? statusCode,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        [FromQuery] string? module = null,
        [FromQuery] string? httpMethod = null,
        [FromQuery] bool businessOnly = false,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetExportLogs(userIdRequired: true, statusCode, fromUtc, toUtc, onlyErrors, module, httpMethod, businessOnly, search, cancellationToken);
        if (logs is null)
        {
            return Unauthorized();
        }

        var bytes = BuildExcel(logs);
        var fileName = BuildSafeFileName($"stoknet-aktivite-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("me/export/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> ExportMyLogsPdf(
        [FromQuery] int? statusCode,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] bool onlyErrors = false,
        [FromQuery] string? module = null,
        [FromQuery] string? httpMethod = null,
        [FromQuery] bool businessOnly = false,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await GetExportLogs(userIdRequired: true, statusCode, fromUtc, toUtc, onlyErrors, module, httpMethod, businessOnly, search, cancellationToken);
        if (logs is null)
        {
            return Unauthorized();
        }

        var bytes = BuildPdf(logs);
        var fileName = BuildSafeFileName($"stoknet-aktivite-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");
        return File(bytes, "application/pdf", fileName);
    }

    private async Task<List<MyActivityLogDto>?> GetExportLogs(
        bool userIdRequired,
        int? statusCode,
        DateTime? fromUtc,
        DateTime? toUtc,
        bool onlyErrors,
        string? module,
        string? httpMethod,
        bool businessOnly,
        string? search,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId) && userIdRequired)
        {
            return null;
        }

        var query = BuildMyLogQuery(userId, fromUtc, toUtc, onlyErrors, module, httpMethod, businessOnly, search);
        if (statusCode.HasValue)
        {
            query = query.Where(x => x.StatusCode == statusCode.Value);
        }

        return await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(2000)
            .Select(ToDtoExpression())
            .ToListAsync(cancellationToken);
    }

    private IQueryable<SystemActivityLog> BuildMyLogQuery(
        Guid userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        bool onlyErrors,
        string? module,
        string? httpMethod,
        bool businessOnly,
        string? search)
    {
        var query = dbContext.SystemActivityLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (currentTenantService.TenantId.HasValue)
        {
            query = query.Where(x => x.TenantAccountId == currentTenantService.TenantId.Value);
        }
        else
        {
            query = query.Where(x => x.TenantAccountId == null);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= toUtc.Value);
        }

        if (onlyErrors)
        {
            query = query.Where(x => x.StatusCode >= 400);
        }

        if (!string.IsNullOrWhiteSpace(httpMethod))
        {
            var normalizedMethod = httpMethod.Trim().ToUpperInvariant();
            query = query.Where(x => x.HttpMethod.ToUpper() == normalizedMethod);
        }

        if (businessOnly)
        {
            query = query.Where(x => !string.IsNullOrWhiteSpace(x.Description)
                || x.HttpMethod.ToUpper() == "APPROVE"
                || x.HttpMethod.ToUpper() == "REJECT");
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            var lowered = module.Trim().ToLowerInvariant();
            query = lowered switch
            {
                "satın alma" or "satinalma" or "purchase" => query.Where(x => x.Path.ToLower().Contains("/purchase-orders")),
                "satış" or "satis" or "sales" => query.Where(x => x.Path.ToLower().Contains("/sales-orders")),
                "stok" or "stock" => query.Where(x => x.Path.ToLower().Contains("/stock-movements")),
                "cari" => query.Where(x => x.Path.ToLower().Contains("/cari-accounts")),
                "bildirim" or "notifications" => query.Where(x => x.Path.ToLower().Contains("/notifications")),
                "aktivite" or "activity" => query.Where(x => x.Path.ToLower().Contains("/activity-logs")),
                "oturum" or "auth" => query.Where(x => x.Path.ToLower().Contains("/auth")),
                "sistem" or "system" => query.Where(x =>
                    !x.Path.ToLower().Contains("/purchase-orders") &&
                    !x.Path.ToLower().Contains("/sales-orders") &&
                    !x.Path.ToLower().Contains("/stock-movements") &&
                    !x.Path.ToLower().Contains("/cari-accounts") &&
                    !x.Path.ToLower().Contains("/notifications") &&
                    !x.Path.ToLower().Contains("/activity-logs") &&
                    !x.Path.ToLower().Contains("/auth")),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.UserName != null && x.UserName.ToLower().Contains(term)) ||
                (x.Description != null && x.Description.ToLower().Contains(term)) ||
                x.Path.ToLower().Contains(term) ||
                x.HttpMethod.ToLower().Contains(term));
        }

        return query;
    }

    private static System.Linq.Expressions.Expression<Func<SystemActivityLog, MyActivityLogDto>> ToDtoExpression() => x => new MyActivityLogDto(
        x.Id,
        x.TenantAccountId,
        x.UserId,
        x.UserName,
        x.Description,
        x.HttpMethod,
        x.Path,
        x.Path.ToLower().Contains("/purchase-orders") ? "Satın alma" :
        x.Path.ToLower().Contains("/sales-orders") ? "Satış" :
        x.Path.ToLower().Contains("/stock-movements") ? "Stok" :
        x.Path.ToLower().Contains("/cari-accounts") ? "Cari" :
        x.Path.ToLower().Contains("/notifications") ? "Bildirim" :
        x.Path.ToLower().Contains("/activity-logs") ? "Aktivite" :
        x.Path.ToLower().Contains("/auth") ? "Oturum" :
        "Sistem",
        x.StatusCode,
        x.DurationMs,
        x.IpAddress,
        x.UserAgent,
        x.OccurredAtUtc);

    private static string BuildCsv(IEnumerable<MyActivityLogDto> logs)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Tarih,Modül,İşlem,Durum,SüreMs,Kullanıcı,IP,Yol,Açıklama");

        foreach (var log in logs)
        {
            builder.AppendLine(string.Join(",",
                Csv(log.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(log.Module),
                Csv(log.HttpMethod),
                Csv(log.StatusCode.ToString()),
                Csv(log.DurationMs.ToString()),
                Csv(log.UserName),
                Csv(log.IpAddress),
                Csv(log.Path),
                Csv(log.Description)));
        }

        return builder.ToString();
    }

    private static byte[] BuildExcel(IReadOnlyList<MyActivityLogDto> logs)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Aktivite");

        var headers = new[] { "Tarih", "Modül", "İşlem", "Durum", "Süre (ms)", "Kullanıcı", "IP", "Yol", "Açıklama" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
            sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        }

        for (var row = 0; row < logs.Count; row++)
        {
            var log = logs[row];
            var excelRow = row + 2;
            sheet.Cell(excelRow, 1).Value = log.OccurredAtUtc;
            sheet.Cell(excelRow, 1).Style.DateFormat.Format = "dd.MM.yyyy HH:mm:ss";
            sheet.Cell(excelRow, 2).Value = log.Module;
            sheet.Cell(excelRow, 3).Value = log.HttpMethod;
            sheet.Cell(excelRow, 4).Value = log.StatusCode;
            sheet.Cell(excelRow, 5).Value = log.DurationMs;
            sheet.Cell(excelRow, 6).Value = log.UserName ?? string.Empty;
            sheet.Cell(excelRow, 7).Value = log.IpAddress ?? string.Empty;
            sheet.Cell(excelRow, 8).Value = log.Path;
            sheet.Cell(excelRow, 9).Value = log.Description ?? string.Empty;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildPdf(IReadOnlyList<MyActivityLogDto> logs)
    {
        var printableLines = new List<string>
        {
            "StokNet Aktivite Gecmisi",
            $"Olusturma Tarihi: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            string.Empty,
            $"Toplam Kayit: {logs.Count}",
            string.Empty,
            "Tarih                Modul         Islem      Durum Sure  Kullanici       Aciklama",
            new string('-', 110)
        };

        foreach (var log in logs)
        {
            printableLines.Add(
                $"{TrimForPdf(log.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm"), 20),-20} " +
                $"{TrimForPdf(ToAscii(log.Module), 13),-13} " +
                $"{TrimForPdf(log.HttpMethod, 10),-10} " +
                $"{TrimForPdf(log.StatusCode.ToString(), 5),-5} " +
                $"{TrimForPdf(log.DurationMs.ToString(), 5),-5} " +
                $"{TrimForPdf(ToAscii(log.UserName), 14),-14} " +
                $"{TrimForPdf(ToAscii(log.Description), 35),-35}");
        }

        return BuildPlainTextPdf(printableLines, maxLinesPerPage: 44);
    }

    private static string Csv(string? value)
    {
        var normalized = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        return $"\"{normalized.Replace("\"", "\"\"")}\"";
    }

    private static string BuildSafeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return sanitized.Replace(' ', '-');
    }

    private static string ToAscii(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return input
            .Replace('ç', 'c').Replace('Ç', 'C')
            .Replace('ğ', 'g').Replace('Ğ', 'G')
            .Replace('ı', 'i').Replace('İ', 'I')
            .Replace('ö', 'o').Replace('Ö', 'O')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ü', 'u').Replace('Ü', 'U');
    }

    private static string TrimForPdf(string? input, int maxLength)
    {
        var value = (input ?? string.Empty).Trim();
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..Math.Max(0, maxLength - 3)] + "...";
    }

    private static byte[] BuildPlainTextPdf(IReadOnlyList<string> lines, int maxLinesPerPage)
    {
        var pages = new List<List<string>>();
        var current = new List<string>();

        foreach (var line in lines)
        {
            current.Add(line);
            if (current.Count >= maxLinesPerPage)
            {
                pages.Add(current);
                current = new List<string>();
            }
        }

        if (current.Count > 0 || pages.Count == 0)
        {
            pages.Add(current);
        }

        var objectContents = new List<byte[]>();
        var pageObjectNumbers = new List<int>();
        var contentObjectNumbers = new List<int>();

        var nextObjectNumber = 3;
        foreach (var _ in pages)
        {
            pageObjectNumbers.Add(nextObjectNumber++);
            contentObjectNumbers.Add(nextObjectNumber++);
        }

        var fontObjectNumber = nextObjectNumber++;
        var kids = string.Join(" ", pageObjectNumbers.Select(x => $"{x} 0 R"));
        var pagesObject = $"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>";

        objectContents.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objectContents.Add(Encoding.ASCII.GetBytes(pagesObject));

        for (var i = 0; i < pages.Count; i++)
        {
            var pageObject = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontObjectNumber} 0 R >> >> /Contents {contentObjectNumbers[i]} 0 R >>";
            objectContents.Add(Encoding.ASCII.GetBytes(pageObject));

            var streamBuilder = new StringBuilder();
            streamBuilder.AppendLine("BT");
            streamBuilder.AppendLine("/F1 9 Tf");
            streamBuilder.AppendLine("11 TL");
            streamBuilder.AppendLine("40 800 Td");

            foreach (var line in pages[i])
            {
                streamBuilder.Append('(').Append(EscapePdfText(ToAscii(line))).AppendLine(") Tj");
                streamBuilder.AppendLine("T*");
            }

            streamBuilder.AppendLine("ET");

            var streamText = streamBuilder.ToString();
            var streamBytes = Encoding.ASCII.GetBytes(streamText);
            var contentPrefix = Encoding.ASCII.GetBytes($"<< /Length {streamBytes.Length} >>\nstream\n");
            var contentSuffix = Encoding.ASCII.GetBytes("endstream");

            using var contentStream = new MemoryStream();
            contentStream.Write(contentPrefix);
            contentStream.Write(streamBytes);
            contentStream.Write(contentSuffix);
            objectContents.Add(contentStream.ToArray());
        }

        objectContents.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        using var pdf = new MemoryStream();
        var offsets = new List<long> { 0 };

        WriteAscii(pdf, "%PDF-1.4\n");

        for (var i = 0; i < objectContents.Count; i++)
        {
            offsets.Add(pdf.Position);
            WriteAscii(pdf, $"{i + 1} 0 obj\n");
            pdf.Write(objectContents[i], 0, objectContents[i].Length);
            WriteAscii(pdf, "\nendobj\n");
        }

        var xrefOffset = pdf.Position;
        WriteAscii(pdf, $"xref\n0 {objectContents.Count + 1}\n");
        WriteAscii(pdf, "0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(pdf, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(pdf, $"trailer\n<< /Size {objectContents.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return pdf.ToArray();
    }

    private static string EscapePdfText(string value)
        => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out userId);
    }
}
