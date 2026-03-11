using System;
using System.Collections.Generic;

namespace ERP.Domain.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Tier1 = "1.Kademe";
    public const string Tier2 = "2.Kademe";
    public const string Tier3 = "3.Kademe";
    public const string AdminOrTier = Admin + "," + Tier1 + "," + Tier2 + "," + Tier3;

    private const string LegacyEmployee = "Employee";

    public static IReadOnlyList<string> TierRoles => [Tier1, Tier2, Tier3];
    public static IReadOnlyList<string> PublicRegistrationRoles => TierRoles;

    public static bool IsTierRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return Matches(role, Tier1)
            || Matches(role, Tier2)
            || Matches(role, Tier3);
    }

    public static bool IsPublicRegistrationRole(string? role)
    {
        return IsTierRole(role);
    }

    public static string NormalizeTierRole(string? role, string defaultRole = Tier1)
    {
        var normalized = (role ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return defaultRole;
        }

        if (Matches(normalized, Tier1) || Matches(normalized, LegacyEmployee))
        {
            return Tier1;
        }

        if (Matches(normalized, Tier2))
        {
            return Tier2;
        }

        if (Matches(normalized, Tier3))
        {
            return Tier3;
        }

        throw new ArgumentException($"Invalid role '{role}'.", nameof(role));
    }

    public static string GetPublicRoleListText()
    {
        return string.Join(", ", PublicRegistrationRoles);
    }

    private static bool Matches(string? role, string expected)
    {
        return !string.IsNullOrWhiteSpace(role)
            && string.Equals(role.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }
}
