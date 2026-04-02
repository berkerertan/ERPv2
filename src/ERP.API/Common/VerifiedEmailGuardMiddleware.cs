using ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ERP.API.Common;

public sealed class VerifiedEmailGuardMiddleware(
    RequestDelegate next,
    IOptions<EmailVerificationOptions> options)
{
    private static readonly string[] AllowedUnverifiedPaths =
    [
        "/api/auth/confirm-email",
        "/api/auth/resend-verification-email",
        "/api/auth/logout"
    ];

    public async Task InvokeAsync(HttpContext context, ErpDbContext dbContext)
    {
        if (!options.Value.EnforceVerifiedUsersForTenantRequests)
        {
            await next(context);
            return;
        }

        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (!user.HasClaim(static x => x.Type == "tenant_id"))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsAllowedForUnverified(path))
        {
            await next(context);
            return;
        }

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
        {
            await next(context);
            return;
        }

        var isEmailConfirmed = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.IsEmailConfirmed)
            .FirstOrDefaultAsync();

        if (isEmailConfirmed)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Email verification required",
            detail = "Please verify your email address to continue.",
            status = StatusCodes.Status403Forbidden
        });
    }

    private static bool IsAllowedForUnverified(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return AllowedUnverifiedPaths.Any(p =>
            path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
