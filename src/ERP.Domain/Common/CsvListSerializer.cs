namespace ERP.Domain.Common;

public static class CsvListSerializer
{
    public static string? Serialize(IEnumerable<string>? values, int maxItems = 100, int maxItemLength = 120)
    {
        if (values is null)
        {
            return null;
        }

        var normalized = values
            .Select(NormalizeToken)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Length > maxItemLength ? x[..maxItemLength] : x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxItems)
            .ToArray();

        return normalized.Length == 0 ? null : string.Join(',', normalized);
    }

    public static IReadOnlyList<string> Deserialize(string? csv, int maxItems = 100)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeToken)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxItems)
            .ToArray();
    }

    public static bool ContainsToken(string? csv, string value)
    {
        var normalized = NormalizeToken(value);
        if (normalized is null)
        {
            return false;
        }

        return Deserialize(csv).Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value
            .Trim()
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace(',', ' ');

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
