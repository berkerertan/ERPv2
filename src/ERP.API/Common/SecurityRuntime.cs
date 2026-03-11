using Microsoft.Extensions.Options;

namespace ERP.API.Common;

public static class SecurityRuntime
{
    public static bool IsAuthorizationEnforced(IServiceProvider services)
    {
        var options = services.GetService<IOptions<SecurityOptions>>();
        return options?.Value.EnforceAuthorization ?? false;
    }
}
