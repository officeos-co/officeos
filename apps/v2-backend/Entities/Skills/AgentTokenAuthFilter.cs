using Microsoft.AspNetCore.Mvc.Filters;

namespace EnterpriseAgentOs.Api.Entities.Skills;

/// <summary>
/// Validates <c>Authorization: Bearer &lt;token&gt;</c> against
/// <see cref="AgentRecord.EncryptedBackendToken"/> for any action on
/// <c>/api/agents/me/*</c>. On success, stashes the resolved agent id in
/// <c>HttpContext.Items["agent-id"]</c>.
///
/// Not a global middleware — applied via an [AgentTokenAuth] attribute so
/// only the agent-facing skill controller is gated, while the dashboard's
/// user-facing routes stay open (v2 has no dashboard auth yet).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AgentTokenAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var header = http.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }
        var presented = header.Substring("Bearer ".Length).Trim();
        if (presented.Length == 0)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }

        var db = http.RequestServices.GetRequiredService<EaosDbContext>();
        var protector = http.RequestServices.GetRequiredService<AgentBackendTokenProtector>();

        // Scan candidate rows. Each token is encrypted with a stable
        // DataProtection purpose, so we unprotect each and compare.
        // Cost is O(n agents) but n is small; a full-text index on
        // ciphertext would not help (ciphertext is non-deterministic).
        var rows = await db.Agents
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.EncryptedBackendToken != null)
            .Select(a => new { a.Id, a.EncryptedBackendToken })
            .ToListAsync();

        Guid? matchedId = null;
        foreach (var row in rows)
        {
            try
            {
                var plaintext = protector.Unprotect(row.EncryptedBackendToken!);
                if (CryptographicEquals(plaintext, presented))
                {
                    matchedId = row.Id;
                    break;
                }
            }
            catch
            {
                // Bad ciphertext — skip this row.
            }
        }

        if (matchedId is null)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }

        http.Items["agent-id"] = matchedId.Value;
    }

    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }
}
