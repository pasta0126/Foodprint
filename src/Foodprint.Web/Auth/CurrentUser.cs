using System.Security.Claims;
using Foodprint.Web.Localization;

namespace Foodprint.Web.Auth;

/// <summary>Scoped view of the signed-in user, sourced from the request principal.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid Id => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new InvalidOperationException("No authenticated user.");

    public Guid? IdOrNull => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? "";
    public string DisplayName => Principal?.FindFirstValue(ClaimTypes.Name) ?? "";
    public bool IsAdmin => Principal?.IsInRole("admin") == true;
    public string Language => Principal?.FindFirstValue(FoodprintRequestLocalization.LanguageClaim) ?? "es";
    public string TimeZoneId => Principal?.FindFirstValue("fp:tz") ?? "Europe/Madrid";
}
