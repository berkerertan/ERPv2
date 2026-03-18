using ERP.API.Common;
using ERP.API.Contracts.Auth;
using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Models;
using ERP.Application.Features.Auth.Commands.BootstrapAdmin;
using ERP.Application.Features.Auth.Commands.Login;
using ERP.Application.Features.Auth.Commands.Register;
using ERP.Application.Features.Auth.Commands.RegisterSaas;
using ERP.Domain.Constants;
using ERP.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
            planConfig?.Features ?? []));
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
}


