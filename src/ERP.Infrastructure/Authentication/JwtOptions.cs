namespace ERP.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ERPv2";
    public string Audience { get; set; } = "ERPv2.Client";
    public string Key { get; set; } = "ChangeThisKey_ERPv2_Development_AtLeast32Chars";
    public int AccessTokenMinutes { get; set; } = 60;
}
