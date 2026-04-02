namespace ERP.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "StokNet";
    public string Audience { get; set; } = "StokNet.Client";
    public string Key { get; set; } = "ChangeThisKey_StokNet_Development_AtLeast32Chars";
    public int AccessTokenMinutes { get; set; } = 60;
}
