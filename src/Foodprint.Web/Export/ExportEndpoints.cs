using System.Globalization;
using System.Text;
using Foodprint.Core.Export;
using Foodprint.Core.Profiles;
using Foodprint.Web.Auth;
using Foodprint.Web.Resources;
using Microsoft.Extensions.Localization;

namespace Foodprint.Web.Export;

public static class ExportEndpoints
{
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/profile/export", async (
            string? from,
            string? to,
            CurrentUser me,
            MealExportService export,
            IStringLocalizer<SharedResource> l,
            CancellationToken ct) =>
        {
            var zone = ProfileService.ResolveZone(me.TimeZoneId);
            var languageLabel = l[$"Language.{me.Language}"].Value;

            var result = await export.BuildAsync(
                me.Id, ParseDate(from), ParseDate(to), zone, languageLabel, ExportStrings.From(l), ct);

            return Results.File(Encoding.UTF8.GetBytes(result.Markdown), "text/markdown; charset=utf-8", result.FileName);
        }).RequireAuthorization();

        return app;
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
}
