namespace ERP.API.Contracts.Auth;

public sealed record LoginRequest(string UserName, string Password);
