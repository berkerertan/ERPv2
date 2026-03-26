using ERP.Application.Abstractions.Persistence;
using ERP.Application.Abstractions.Security;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Models;
using ERP.Domain.Constants;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using MediatR;
using System.Text;

namespace ERP.Application.Features.Auth.Commands.RegisterSaas;

public sealed class RegisterSaasCommandHandler(
    IUserRepository userRepository,
    ITenantAccountRepository tenantAccountRepository,
    ISubscriptionPlanService subscriptionPlanService,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<RegisterSaasCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterSaasCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.GetByUserNameAsync(request.UserName, cancellationToken) is not null)
        {
            throw new ConflictException("Username already exists.");
        }

        if (await userRepository.GetByEmailAsync(request.Email, cancellationToken) is not null)
        {
            throw new ConflictException("Email already exists.");
        }

        var planConfig = await subscriptionPlanService.GetPlanConfigAsync(request.Plan, cancellationToken);
        if (!planConfig.IsActive)
        {
            throw new ConflictException("Selected subscription plan is not currently available.");
        }

        var tenantCode = await GenerateUniqueTenantCodeAsync(request.CompanyName, cancellationToken);
        var now = DateTime.UtcNow;

        var tenant = new TenantAccount
        {
            Name = request.CompanyName.Trim(),
            Code = tenantCode,
            Plan = request.Plan,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionStartAtUtc = now,
            SubscriptionEndAtUtc = now.AddMonths(1),
            MaxUsers = planConfig.MaxUsers
        };

        await tenantAccountRepository.AddAsync(tenant, cancellationToken);

        var user = new AppUser
        {
            TenantAccountId = tenant.Id,
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = planConfig.AssignedRole,
            RefreshToken = jwtTokenService.GenerateRefreshToken(),
            RefreshTokenExpiresAtUtc = now.AddDays(7)
        };

        await userRepository.AddAsync(user, cancellationToken);

        var token = jwtTokenService.GenerateAccessToken(user, tenant, planConfig.Features);

        return new AuthResponse(
            token.AccessToken,
            user.RefreshToken ?? string.Empty,
            token.ExpiresAtUtc,
            user.Role,
            user.UserName,
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            tenant.SubscriptionStatus,
            planConfig.Features,
            token.ExpiresAtUtc,
            user.RefreshTokenExpiresAtUtc);
    }

    private async Task<string> GenerateUniqueTenantCodeAsync(string companyName, CancellationToken cancellationToken)
    {
        var baseCode = Slugify(companyName);
        if (string.IsNullOrWhiteSpace(baseCode))
        {
            baseCode = "tenant";
        }

        var code = baseCode;
        var suffix = 1;

        while (await tenantAccountRepository.GetByCodeAsync(code, cancellationToken) is not null)
        {
            code = $"{baseCode}-{suffix++}";
        }

        return code;
    }

    private static string Slugify(string input)
    {
        var normalized = (input ?? string.Empty).Trim().ToLowerInvariant();
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                continue;
            }

            if (ch == ' ' || ch == '-' || ch == '_')
            {
                sb.Append('-');
            }
        }

        var slug = sb.ToString().Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug;
    }
}
