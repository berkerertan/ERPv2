namespace ERP.Application.Common.Models;

public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);
