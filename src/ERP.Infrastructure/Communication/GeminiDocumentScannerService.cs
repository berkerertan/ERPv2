using ERP.Application.Abstractions.DocumentScanner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ERP.Infrastructure.Communication;

public sealed class GeminiDocumentScannerService(
    HttpClient httpClient,
    IOptions<GeminiOptions> geminiOptions,
    IOptions<ClaudeOptions> claudeOptions,
    ILogger<GeminiDocumentScannerService> logger) : IDocumentScannerService
{
    private const string ExtractionPrompt =
        "OCR and extract accounting document fields from this image. " +
        "Return ONLY compact valid JSON without markdown. " +
        "Schema: " +
        "{vendorName:string|null,vendorTaxId:string|null,documentDate:string|null,documentNumber:string|null,documentType:string|null,currency:string|null,items:[{description:string,quantity:number|null,unit:string|null,unitPrice:number|null,totalPrice:number|null,taxRate:number|null}],subtotal:number|null,taxAmount:number|null,total:number|null,errorMessage:string|null}. " +
        "documentType must be one of: invoice, waybill, receipt, other. " +
        "Use null for unknown fields.";

    private static readonly JsonSerializerOptions ParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly GeminiOptions _geminiOptions = geminiOptions.Value;
    private readonly ClaudeOptions _claudeOptions = claudeOptions.Value;

    public Task<DocumentScanResult> AnalyzeAsync(ScanDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var provider = NormalizeProvider(request.Provider);

        return provider switch
        {
            "gemini" => AnalyzeWithGeminiAsync(request, provider, cancellationToken),
            "claude" => AnalyzeWithClaudeAsync(request, provider, cancellationToken),
            _ => Task.FromResult(CreateError(provider, $"'{provider}' provider is not configured on server."))
        };
    }

    private async Task<DocumentScanResult> AnalyzeWithGeminiAsync(
        ScanDocumentRequest request,
        string provider,
        CancellationToken cancellationToken)
    {
        if (!_geminiOptions.Enabled || string.IsNullOrWhiteSpace(_geminiOptions.ApiKey))
        {
            return CreateError(provider, "Gemini is not configured on server.");
        }

        if (string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return CreateError(provider, "ImageBase64 is required.");
        }

        var mimeType = string.IsNullOrWhiteSpace(request.MimeType)
            ? "image/jpeg"
            : request.MimeType.Trim();

        var endpoint = BuildGeminiEndpoint();
        var payload = BuildGeminiPayload(request.ImageBase64.Trim(), mimeType);

        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };

            using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = TryExtractApiError(raw)
                    ?? $"Gemini request failed with status code {(int)response.StatusCode}.";
                logger.LogWarning("Gemini analyze failed ({StatusCode}): {Message}", (int)response.StatusCode, message);
                return CreateError(provider, message);
            }

            var modelText = TryExtractGeminiText(raw, out var modelError);
            if (string.IsNullOrWhiteSpace(modelText))
            {
                return CreateError(provider, modelError ?? "Gemini returned an empty response.");
            }

            return ParseModelResult(provider, modelText, "Gemini");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gemini document analysis failed unexpectedly.");
            return CreateError(provider, "Document analysis failed unexpectedly.");
        }
    }

    private async Task<DocumentScanResult> AnalyzeWithClaudeAsync(
        ScanDocumentRequest request,
        string provider,
        CancellationToken cancellationToken)
    {
        if (!_claudeOptions.Enabled || string.IsNullOrWhiteSpace(_claudeOptions.ApiKey))
        {
            return CreateError(provider, "Claude is not configured on server.");
        }

        if (string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return CreateError(provider, "ImageBase64 is required.");
        }

        var mimeType = NormalizeClaudeMimeType(request.MimeType);
        var endpoint = BuildClaudeEndpoint();
        var payload = BuildClaudePayload(request.ImageBase64.Trim(), mimeType);

        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };

            requestMessage.Headers.Add("x-api-key", _claudeOptions.ApiKey.Trim());
            requestMessage.Headers.Add("anthropic-version", BuildClaudeApiVersion());

            using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var message = TryExtractApiError(raw)
                    ?? $"Claude request failed with status code {(int)response.StatusCode}.";
                logger.LogWarning("Claude analyze failed ({StatusCode}): {Message}", (int)response.StatusCode, message);
                return CreateError(provider, message);
            }

            var modelText = TryExtractClaudeText(raw, out var modelError);
            if (string.IsNullOrWhiteSpace(modelText))
            {
                return CreateError(provider, modelError ?? "Claude returned an empty response.");
            }

            return ParseModelResult(provider, modelText, "Claude");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claude document analysis failed unexpectedly.");
            return CreateError(provider, "Document analysis failed unexpectedly.");
        }
    }

    private object BuildGeminiPayload(string imageBase64, string mimeType)
    {
        return new
        {
            contents =
                new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = ExtractionPrompt },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = imageBase64
                                }
                            }
                        }
                    }
                },
            generationConfig = new
            {
                temperature = 0.1
            }
        };
    }

    private object BuildClaudePayload(string imageBase64, string mimeType)
    {
        var model = string.IsNullOrWhiteSpace(_claudeOptions.Model)
            ? "claude-sonnet-4-20250514"
            : _claudeOptions.Model.Trim();

        return new
        {
            model,
            max_tokens = Math.Clamp(_claudeOptions.MaxTokens, 1024, 8192),
            temperature = 0.1,
            messages =
                new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image",
                                source = new
                                {
                                    type = "base64",
                                    media_type = mimeType,
                                    data = imageBase64
                                }
                            },
                            new
                            {
                                type = "text",
                                text = ExtractionPrompt
                            }
                        }
                    }
                }
        };
    }

    private string BuildGeminiEndpoint()
    {
        var endpoint = string.IsNullOrWhiteSpace(_geminiOptions.Endpoint)
            ? "https://generativelanguage.googleapis.com/v1beta/models"
            : _geminiOptions.Endpoint.TrimEnd('/');

        var model = string.IsNullOrWhiteSpace(_geminiOptions.Model)
            ? "gemini-2.0-flash"
            : _geminiOptions.Model.Trim();

        var apiKey = Uri.EscapeDataString(_geminiOptions.ApiKey.Trim());
        return $"{endpoint}/{model}:generateContent?key={apiKey}";
    }

    private string BuildClaudeEndpoint()
    {
        var endpoint = string.IsNullOrWhiteSpace(_claudeOptions.Endpoint)
            ? "https://api.anthropic.com/v1/messages"
            : _claudeOptions.Endpoint.Trim();

        return endpoint;
    }

    private string BuildClaudeApiVersion()
    {
        return string.IsNullOrWhiteSpace(_claudeOptions.ApiVersion)
            ? "2023-06-01"
            : _claudeOptions.ApiVersion.Trim();
    }

    private DocumentScanResult ParseModelResult(string provider, string modelText, string providerLabel)
    {
        var jsonPayload = ExtractJsonPayload(modelText);
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            logger.LogWarning("{Provider} response did not include parseable JSON: {Response}", providerLabel, modelText);
            return CreateError(provider, $"{providerLabel} response format is invalid.");
        }

        ParsedScanResult? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ParsedScanResult>(jsonPayload, ParseOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{Provider} JSON parse failed.", providerLabel);
            return CreateError(provider, $"{providerLabel} response could not be parsed.");
        }

        if (parsed is null)
        {
            return CreateError(provider, $"{providerLabel} response could not be parsed.");
        }

        var items = (parsed.Items ?? [])
            .Select(MapItem)
            .Where(x => !string.IsNullOrWhiteSpace(x.Description))
            .ToList();

        var subtotal = parsed.Subtotal ?? (items.Count > 0 ? items.Sum(x => x.TotalPrice) : null);
        var taxAmount = parsed.TaxAmount;
        var total = parsed.Total;

        if (total is null && subtotal is not null && taxAmount is not null)
        {
            total = subtotal.Value + taxAmount.Value;
        }

        return new DocumentScanResult(
            NormalizeNullable(parsed.VendorName),
            NormalizeNullable(parsed.VendorTaxId),
            NormalizeNullable(parsed.DocumentDate),
            NormalizeNullable(parsed.DocumentNumber),
            NormalizeDocumentType(parsed.DocumentType),
            NormalizeCurrency(parsed.Currency),
            items,
            subtotal,
            taxAmount,
            total,
            provider,
            NormalizeNullable(parsed.ErrorMessage));
    }

    private static string NormalizeProvider(string? provider)
    {
        return string.IsNullOrWhiteSpace(provider)
            ? "gemini"
            : provider.Trim().ToLowerInvariant();
    }

    private static string NormalizeClaudeMimeType(string? mimeType)
    {
        var normalized = string.IsNullOrWhiteSpace(mimeType)
            ? "image/jpeg"
            : mimeType.Trim().ToLowerInvariant();

        return normalized switch
        {
            "image/jpeg" => "image/jpeg",
            "image/jpg" => "image/jpeg",
            "image/png" => "image/png",
            "image/webp" => "image/webp",
            "image/gif" => "image/gif",
            _ => "image/jpeg"
        };
    }

    private static DocumentScanResult CreateError(string provider, string message)
    {
        return new DocumentScanResult(
            null,
            null,
            null,
            null,
            null,
            null,
            [],
            null,
            null,
            null,
            provider,
            message);
    }

    private static string? TryExtractApiError(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;

            if (root.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.ValueKind == JsonValueKind.Object
                    && errorElement.TryGetProperty("message", out var nestedMessageElement))
                {
                    return NormalizeNullable(nestedMessageElement.GetString());
                }

                if (errorElement.ValueKind == JsonValueKind.String)
                {
                    return NormalizeNullable(errorElement.GetString());
                }
            }

            if (root.TryGetProperty("message", out var messageElement))
            {
                return NormalizeNullable(messageElement.GetString());
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static string? TryExtractGeminiText(string raw, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;

            if (root.TryGetProperty("error", out var errorElement)
                && errorElement.TryGetProperty("message", out var messageElement))
            {
                errorMessage = NormalizeNullable(messageElement.GetString()) ?? "Gemini returned an error.";
                return null;
            }

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
            {
                errorMessage = "Gemini returned no candidates.";
                return null;
            }

            var sb = new StringBuilder();
            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content))
                {
                    continue;
                }

                if (!content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text))
                    {
                        var value = text.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            if (sb.Length > 0)
                            {
                                sb.Append('\n');
                            }

                            sb.Append(value);
                        }
                    }
                }
            }

            if (sb.Length == 0)
            {
                errorMessage = "Gemini returned no text content.";
                return null;
            }

            return sb.ToString();
        }
        catch
        {
            errorMessage = "Gemini response could not be parsed.";
            return null;
        }
    }

    private static string? TryExtractClaudeText(string raw, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;

            if (root.TryGetProperty("error", out var errorElement)
                && errorElement.ValueKind == JsonValueKind.Object
                && errorElement.TryGetProperty("message", out var messageElement))
            {
                errorMessage = NormalizeNullable(messageElement.GetString()) ?? "Claude returned an error.";
                return null;
            }

            if (!root.TryGetProperty("content", out var contentArray)
                || contentArray.ValueKind != JsonValueKind.Array
                || contentArray.GetArrayLength() == 0)
            {
                errorMessage = "Claude returned no content.";
                return null;
            }

            var sb = new StringBuilder();
            foreach (var item in contentArray.EnumerateArray())
            {
                if (!item.TryGetProperty("type", out var typeElement)
                    || !string.Equals(typeElement.GetString(), "text", StringComparison.OrdinalIgnoreCase)
                    || !item.TryGetProperty("text", out var textElement))
                {
                    continue;
                }

                var value = textElement.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append('\n');
                }

                sb.Append(value);
            }

            if (sb.Length == 0)
            {
                errorMessage = "Claude returned no text content.";
                return null;
            }

            if (root.TryGetProperty("stop_reason", out var stopReasonElement)
                && string.Equals(stopReasonElement.GetString(), "max_tokens", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(ExtractJsonPayload(sb.ToString())))
            {
                errorMessage = "Claude response was truncated by token limit. Increase Claude max tokens.";
                return null;
            }

            return sb.ToString();
        }
        catch
        {
            errorMessage = "Claude response could not be parsed.";
            return null;
        }
    }

    private static string? ExtractJsonPayload(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Trim();

        if (normalized.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = normalized.IndexOf('\n');
            if (firstNewLine >= 0)
            {
                normalized = normalized[(firstNewLine + 1)..];
            }

            if (normalized.EndsWith("```", StringComparison.Ordinal))
            {
                normalized = normalized[..^3];
            }

            normalized = normalized.Trim();
        }

        var start = normalized.IndexOf('{');
        var end = normalized.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return normalized[start..(end + 1)];
    }

    private static ScannedLineItem MapItem(ParsedLineItem item)
    {
        var description = NormalizeNullable(item.Description) ?? string.Empty;
        var quantity = item.Quantity is null || item.Quantity.Value <= 0 ? 1 : item.Quantity.Value;
        var unit = NormalizeNullable(item.Unit) ?? "adet";
        var unitPrice = item.UnitPrice is null || item.UnitPrice.Value < 0 ? 0 : item.UnitPrice.Value;
        var totalPrice = item.TotalPrice ?? 0;

        if (totalPrice <= 0)
        {
            totalPrice = quantity * unitPrice;
        }

        return new ScannedLineItem(
            description,
            quantity,
            unit,
            unitPrice,
            totalPrice,
            item.TaxRate);
    }

    private static string? NormalizeDocumentType(string? value)
    {
        var normalized = NormalizeNullable(value)?.ToLowerInvariant();
        return normalized switch
        {
            "invoice" => "invoice",
            "waybill" => "waybill",
            "receipt" => "receipt",
            "other" => "other",
            null => null,
            _ => "other"
        };
    }

    private static string? NormalizeCurrency(string? value)
    {
        var normalized = NormalizeNullable(value)?.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= 8
            ? normalized
            : normalized[..8];
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed class ParsedScanResult
    {
        public string? VendorName { get; set; }
        public string? VendorTaxId { get; set; }
        public string? DocumentDate { get; set; }
        public string? DocumentNumber { get; set; }
        public string? DocumentType { get; set; }
        public string? Currency { get; set; }
        public List<ParsedLineItem>? Items { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? Total { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private sealed class ParsedLineItem
    {
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? TaxRate { get; set; }
    }
}
