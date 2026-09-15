using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FieldEvents.Server.Auth;

public class AgentApiKeyOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "AgentApiKey";
    public string ExpectedKey { get; set; } = string.Empty;
}

/// <summary>
/// Authenticates the Agent's own connection to EventsHub - distinct from end-user JWT auth.
/// A single shared key is enough for this exercise's scope (one trusted Agent process); a
/// multi-agent deployment would give each Agent instance its own key looked up from a store.
/// </summary>
public class AgentApiKeyAuthenticationHandler(
    IOptionsMonitor<AgentApiKeyOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AgentApiKeyOptions>(options, logger, encoder)
{
    private const string HeaderName = "X-Agent-Key";
    private const string QueryName = "agentKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var providedKey = Request.Headers[HeaderName].FirstOrDefault()
            ?? Request.Query[QueryName].FirstOrDefault();

        if (string.IsNullOrEmpty(providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing agent API key."));
        }

        if (!string.Equals(providedKey, Options.ExpectedKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid agent API key."));
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "agent-service"), new Claim(ClaimTypes.Role, "AgentService") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
