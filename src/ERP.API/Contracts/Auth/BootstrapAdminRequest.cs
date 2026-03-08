namespace ERP.API.Contracts.Auth;

public sealed record BootstrapAdminRequest(string UserName, string Email, string Password);
