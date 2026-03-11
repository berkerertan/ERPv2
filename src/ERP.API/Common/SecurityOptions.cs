namespace ERP.API.Common;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool EnforceAuthorization { get; init; }
}
