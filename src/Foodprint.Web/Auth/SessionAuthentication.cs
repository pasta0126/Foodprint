using System.Security.Claims;
using System.Text.Encodings.Web;
using Foodprint.Core.Auth;
using Foodprint.Web.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Foodprint.Web.Auth;

public static class SessionAuth
{
    public const string Scheme = "Foodprint";
    public const string CookieName = "fp_session";
    public const string AdminPolicy = "AdminOnly";
}

public sealed class SessionAuthOptions : AuthenticationSchemeOptions;

/// <summary>Resolves the <c>fp_session</c> cookie to a principal on every request.</summary>
public sealed class SessionAuthHandler(
    IOptionsMonitor<SessionAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AuthService auth)
    : AuthenticationHandler<SessionAuthOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(SessionAuth.CookieName, out var token) || string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        var user = await auth.ResolveSessionAsync(token, Context.RequestAborted);
        if (user is null)
        {
            Response.Cookies.Delete(SessionAuth.CookieName);
            return AuthenticateResult.Fail("Invalid or expired session");
        }

        var identity = new ClaimsIdentity(SessionAuth.Scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(FoodprintRequestLocalization.LanguageClaim, user.Language));
        identity.AddClaim(new Claim("fp:tz", user.TimeZoneId));
        if (user.IsAdmin)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
        }

        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SessionAuth.Scheme));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (WantsJson())
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        var returnUrl = Request.Path + Request.QueryString;
        Response.Redirect($"/sign-in?returnUrl={UrlEncoder.Default.Encode(returnUrl)}");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = WantsJson() ? StatusCodes.Status403Forbidden : StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    private bool WantsJson()
    {
        var accept = Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
