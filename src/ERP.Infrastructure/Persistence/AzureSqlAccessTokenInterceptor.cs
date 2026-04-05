using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace ERP.Infrastructure.Persistence;

public sealed class AzureSqlAccessTokenInterceptor : DbConnectionInterceptor
{
    private static readonly TokenRequestContext TokenRequestContext =
        new(["https://database.windows.net//.default"]);

    private readonly TokenCredential _credential;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private AccessToken _cachedToken;
    private bool _hasToken;

    public AzureSqlAccessTokenInterceptor()
    {
        _credential = new DefaultAzureCredential();
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        ApplyTokenIfAzureSql(connection, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        await ApplyTokenIfAzureSql(connection, cancellationToken);
        return result;
    }

    private async Task ApplyTokenIfAzureSql(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection is not SqlConnection sqlConnection)
        {
            return;
        }

        if (!IsAzureSqlConnection(sqlConnection.ConnectionString))
        {
            return;
        }

        sqlConnection.AccessToken = await GetAccessTokenAsync(cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_hasToken && _cachedToken.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return _cachedToken.Token;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_hasToken && _cachedToken.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return _cachedToken.Token;
            }

            _cachedToken = await _credential.GetTokenAsync(TokenRequestContext, cancellationToken);
            _hasToken = true;
            return _cachedToken.Token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static bool IsAzureSqlConnection(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return builder.DataSource.Contains(".database.windows.net", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

