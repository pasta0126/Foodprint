using Foodprint.Core.Meals;
using Foodprint.Web.Auth;

namespace Foodprint.Web.Components.Meals;

public static class MealEndpoints
{
    public static IEndpointRouteBuilder MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/entries/{id:guid}/delete", async (Guid id, CurrentUser me, MealEntryService entries) =>
        {
            await entries.DeleteAsync(me.Id, id);
            return Results.LocalRedirect("/");
        }).RequireAuthorization();

        return app;
    }
}
