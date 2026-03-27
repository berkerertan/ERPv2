using ERP.API.Common;
using ERP.API.Contracts.Auth;
using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Models;
using ERP.Application.Features.Auth.Commands.BootstrapAdmin;
using ERP.Application.Features.Auth.Commands.Login;
using ERP.Application.Features.Auth.Commands.Register;
using ERP.Application.Features.Auth.Commands.RegisterSaas;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IMediator mediator,
    ISubscriptionPlanService subscriptionPlanService,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher,
    ErpDbContext dbContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("subscription-plans")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPlanOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanOptionDto>>> GetSubscriptionPlans(CancellationToken cancellationToken)
    {
        var plans = await subscriptionPlanService.GetAllPlansAsync(onlyActive: false, cancellationToken);

        var response = plans
            .Select(x => new SubscriptionPlanOptionDto(
                x.Plan,
                x.DisplayName,
                x.AssignedRole,
                x.MonthlyPrice,
                x.MaxUsers,
                x.IsActive,
                x.Features))
            .ToList();

        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("bootstrap-admin")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> BootstrapAdmin(
        [FromBody] BootstrapAdminRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BootstrapAdminCommand(request.UserName, request.Email, request.Password);
        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserRegistrationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserRegistrationResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.UserName, request.Email, request.Password, request.Role);
        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register-saas")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> RegisterSaas(
        [FromBody] RegisterSaasRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterSaasCommand(
            request.UserName,
            request.Email,
            request.Password,
            request.CompanyName,
            request.Plan);

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.UserName, request.Password);
        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var refreshToken = (request.RefreshToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken, cancellationToken);
        if (user is null || user.RefreshTokenExpiresAtUtc is null || user.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            return Unauthorized("Refresh token is invalid or expired.");
        }

        var tenant = user.TenantAccountId.HasValue
            ? await dbContext.TenantAccounts.FirstOrDefaultAsync(x => x.Id == user.TenantAccountId.Value, cancellationToken)
            : null;

        var planConfig = tenant is null
            ? null
            : await subscriptionPlanService.GetPlanConfigAsync(tenant.Plan, cancellationToken);

        user.RefreshToken = jwtTokenService.GenerateRefreshToken();
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtTokenService.GenerateAccessToken(user, tenant, planConfig?.Features);
        return Ok(new AuthResponse(
            token.AccessToken,
            user.RefreshToken,
            token.ExpiresAtUtc,
            user.Role,
            user.UserName,
            tenant?.Id,
            tenant?.Name,
            tenant?.Plan,
            tenant?.SubscriptionStatus,
            planConfig?.Features ?? [],
            token.ExpiresAtUtc,
            user.RefreshTokenExpiresAtUtc));
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var tenant = user.TenantAccountId.HasValue
            ? await dbContext.TenantAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.TenantAccountId.Value, cancellationToken)
            : null;

        var planConfig = tenant is null
            ? null
            : await subscriptionPlanService.GetPlanConfigAsync(tenant.Plan, cancellationToken);

        var features = planConfig?.Features ?? [];
        return Ok(new CurrentUserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Role,
            tenant?.Id,
            tenant?.Name,
            tenant?.Code,
            string.Equals(user.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) && tenant is null,
            planConfig?.DisplayName,
            tenant?.SubscriptionStatus.ToString(),
            features));
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpPut("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> UpdateMe(
        [FromBody] UpdateCurrentUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var normalizedUserName = (request.UserName ?? string.Empty).Trim();
        var normalizedEmail = (request.Email ?? string.Empty).Trim();

        if (normalizedUserName.Length is < 3 or > 50)
        {
            return BadRequest("UserName must be between 3 and 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequest("Email is required.");
        }

        if (normalizedEmail.Length > 100)
        {
            return BadRequest("Email cannot exceed 100 characters.");
        }

        if (!new EmailAddressAttribute().IsValid(normalizedEmail))
        {
            return BadRequest("Email format is invalid.");
        }

        var isProtectedSeedAccount = IsProtectedSeedAccount(user);
        var identityChanged =
            !string.Equals(user.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);

        if (isProtectedSeedAccount && identityChanged)
        {
            return BadRequest("This demo/system account cannot change username or email.");
        }

        var duplicateUserNameExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id != userId && x.UserName.ToLower() == normalizedUserName.ToLower(), cancellationToken);

        if (duplicateUserNameExists)
        {
            return Conflict("Username already exists.");
        }

        var duplicateEmailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id != userId && x.Email.ToLower() == normalizedEmail.ToLower(), cancellationToken);

        if (duplicateEmailExists)
        {
            return Conflict("Email already exists.");
        }

        user.UserName = normalizedUserName;
        user.Email = normalizedEmail;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var tenant = user.TenantAccountId.HasValue
            ? await dbContext.TenantAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.TenantAccountId.Value, cancellationToken)
            : null;

        var planConfig = tenant is null
            ? null
            : await subscriptionPlanService.GetPlanConfigAsync(tenant.Plan, cancellationToken);

        var features = planConfig?.Features ?? [];
        return Ok(new CurrentUserDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Role,
            tenant?.Id,
            tenant?.Name,
            tenant?.Code,
            string.Equals(user.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) && tenant is null,
            planConfig?.DisplayName,
            tenant?.SubscriptionStatus.ToString(),
            features));
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeMyPassword(
        [FromBody] ChangeCurrentUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var currentPassword = (request.CurrentPassword ?? string.Empty).Trim();
        var newPassword = (request.NewPassword ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return BadRequest("CurrentPassword and NewPassword are required.");
        }

        if (newPassword.Length < 6)
        {
            return BadRequest("NewPassword must be at least 6 characters.");
        }

        if (IsProtectedSeedAccount(user))
        {
            return BadRequest("Demo/system accounts cannot change password.");
        }

        if (!passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            return BadRequest("Current password is incorrect.");
        }

        if (passwordHasher.Verify(newPassword, user.PasswordHash))
        {
            return BadRequest("NewPassword must be different from current password.");
        }

        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        Guid.TryParse(sub, out var userId);

        var user = userId != Guid.Empty
            ? await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            : null;

        if (user is null && !string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            user = await dbContext.Users.FirstOrDefaultAsync(x => x.RefreshToken == request.RefreshToken, cancellationToken);
        }

        if (user is null)
        {
            return NoContent();
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /* ═══════════════════════════════════════════════════════════════
       2FA (İki Faktörlü Kimlik Doğrulama)
    ═══════════════════════════════════════════════════════════════ */

    [RequirePolicy("TierUserOrAdmin")]
    [HttpGet("2fa/status")]
    [ProducesResponseType(typeof(TwoFactorStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorStatusResponse>> GetTwoFactorStatus(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();

        return Ok(new TwoFactorStatusResponse(user.TwoFactorEnabled, !string.IsNullOrEmpty(user.TwoFactorSecretKey)));
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpPost("2fa/setup")]
    [ProducesResponseType(typeof(TwoFactorSetupResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();

        if (user.TwoFactorEnabled)
        {
            return BadRequest("2FA is already enabled. Disable it first.");
        }

        // Generate a 20-byte (160-bit) secret key and encode it as Base32
        var keyBytes = RandomNumberGenerator.GetBytes(20);
        var sharedKey = Base32Encode(keyBytes);

        user.TwoFactorSecretKey = sharedKey;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var issuer = "ERPv2";
        var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email)}?secret={sharedKey}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

        return Ok(new TwoFactorSetupResponse(sharedKey, qrCodeUri));
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpPost("2fa/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorVerifyRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecretKey))
        {
            return BadRequest("Call 2fa/setup first.");
        }

        var code = (request.Code ?? string.Empty).Trim();
        if (code.Length != 6 || !VerifyTotpCode(user.TwoFactorSecretKey, code))
        {
            return BadRequest("Invalid verification code.");
        }

        user.TwoFactorEnabled = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpPost("2fa/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorVerifyRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();

        if (!user.TwoFactorEnabled)
        {
            return BadRequest("2FA is not enabled.");
        }

        var code = (request.Code ?? string.Empty).Trim();
        if (code.Length != 6 || !VerifyTotpCode(user.TwoFactorSecretKey!, code))
        {
            return BadRequest("Invalid verification code.");
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecretKey = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /* ═══════════════════════════════════════════════════════════════
       Bildirim Tercihleri
    ═══════════════════════════════════════════════════════════════ */

    [RequirePolicy("TierUserOrAdmin")]
    [HttpGet("notification-preferences")]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPreferencesDto>> GetNotificationPreferences(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken, tracking: false);
        if (user is null) return Unauthorized();

        return Ok(new NotificationPreferencesDto(
            user.NotifEmailInvoice,
            user.NotifEmailPayment,
            user.NotifEmailReminder,
            user.NotifEmailMarketing,
            user.NotifPushEnabled,
            user.NotifPushOrderStatus,
            user.NotifPushStockAlert));
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpPut("notification-preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();

        if (request.EmailInvoice.HasValue) user.NotifEmailInvoice = request.EmailInvoice.Value;
        if (request.EmailPayment.HasValue) user.NotifEmailPayment = request.EmailPayment.Value;
        if (request.EmailReminder.HasValue) user.NotifEmailReminder = request.EmailReminder.Value;
        if (request.EmailMarketing.HasValue) user.NotifEmailMarketing = request.EmailMarketing.Value;
        if (request.PushEnabled.HasValue) user.NotifPushEnabled = request.PushEnabled.Value;
        if (request.PushOrderStatus.HasValue) user.NotifPushOrderStatus = request.PushOrderStatus.Value;
        if (request.PushStockAlert.HasValue) user.NotifPushStockAlert = request.PushStockAlert.Value;

        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /* ═══════════════════════════════════════════════════════════════
       Aktif Oturumlar
    ═══════════════════════════════════════════════════════════════ */

    [RequirePolicy("TierUserOrAdmin")]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<ActiveSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ActiveSessionDto>>> GetActiveSessions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var sessions = await dbContext.UserSessions.AsNoTracking()
            .Where(x => x.UserId == userId.Value && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.LastActiveUtc)
            .ToListAsync(cancellationToken);

        // Determine which session is the current one via the Bearer token's refresh token
        var currentRefreshToken = GetCurrentRefreshTokenFromHeader();

        var result = sessions.Select(s => new ActiveSessionDto(
            s.Id,
            s.DeviceName,
            s.IpAddress,
            s.Location,
            s.LastActiveUtc,
            s.RefreshToken == currentRefreshToken
        )).ToList();

        return Ok(result);
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId.Value, cancellationToken);

        if (session is null)
        {
            return NotFound();
        }

        session.MarkAsDeleted();
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [RequirePolicy("TierUserOrAdmin")]
    [HttpPost("sessions/revoke-others")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeOtherSessions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var currentRefreshToken = GetCurrentRefreshTokenFromHeader();

        var otherSessions = await dbContext.UserSessions
            .Where(x => x.UserId == userId.Value && x.RefreshToken != currentRefreshToken && x.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var session in otherSessions)
        {
            session.MarkAsDeleted();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /* ═══════════════════════════════════════════════════════════════
       Private Helpers
    ═══════════════════════════════════════════════════════════════ */

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private async Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken, bool tracking = true)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return null;

        var query = tracking ? dbContext.Users.AsQueryable() : dbContext.Users.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
    }

    private string? GetCurrentRefreshTokenFromHeader()
    {
        // The current session's refresh token can be sent as a custom header
        // or we can identify by the access token's jti claim
        return Request.Headers.TryGetValue("X-Refresh-Token", out var values) ? values.FirstOrDefault() : null;
    }

    private static bool IsProtectedSeedAccount(AppUser user)
    {
        var userName = (user.UserName ?? string.Empty).Trim();
        var email = (user.Email ?? string.Empty).Trim();

        return string.Equals(userName, "platform.admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(userName, "demo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(userName, "demo.tier1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(userName, "demo.tier2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(email, "platform.admin@erp.local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(email, "demo@erp.local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(email, "demo.tier1@erp.local", StringComparison.OrdinalIgnoreCase)
            || string.Equals(email, "demo.tier2@erp.local", StringComparison.OrdinalIgnoreCase);
    }

    /* ─── TOTP Helpers ──────────────────────────────────────────── */

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var value = 0;
        var output = new System.Text.StringBuilder();

        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                output.Append(alphabet[(value >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            output.Append(alphabet[(value << (5 - bits)) & 31]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string encoded)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var bits = 0;
        var value = 0;

        foreach (var c in encoded.ToUpperInvariant())
        {
            if (c == '=' || c == ' ') continue;
            var idx = alphabet.IndexOf(c);
            if (idx < 0) continue;
            value = (value << 5) | idx;
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)((value >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return bytes.ToArray();
    }

    private static bool VerifyTotpCode(string secretKey, string code)
    {
        if (!long.TryParse(code, out _) || code.Length != 6) return false;

        var keyBytes = Base32Decode(secretKey);
        var timeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        // Allow 1 step before/after for clock drift
        for (var i = -1; i <= 1; i++)
        {
            var stepBytes = BitConverter.GetBytes((long)(timeStep + i));
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(stepBytes);
            }

            using var hmac = new HMACSHA1(keyBytes);
            var hash = hmac.ComputeHash(stepBytes);
            var offset = hash[^1] & 0x0F;
            var binary =
                ((hash[offset] & 0x7F) << 24) |
                ((hash[offset + 1] & 0xFF) << 16) |
                ((hash[offset + 2] & 0xFF) << 8) |
                (hash[offset + 3] & 0xFF);

            var otp = (binary % 1_000_000).ToString("D6");
            if (otp == code) return true;
        }

        return false;
    }
}
