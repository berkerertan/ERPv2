namespace ERP.Application.Common.Models;

public sealed record UserRegistrationResponse(Guid UserId, string UserName, string Role);
