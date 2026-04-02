using System.Security.Cryptography;
using System.Text;

namespace ERP.Application.Common.Security;

public static class EmailVerificationTokenCodec
{
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return ToBase64Url(bytes);
    }

    public static string HashToken(string token)
    {
        var normalized = (token ?? string.Empty).Trim();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash);
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
