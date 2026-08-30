using Foodprint.Core.Meals;
using Foodprint.Web.Auth;

namespace Foodprint.Web.Components.Meals;

public static class FavoriteEndpoints
{
    public static IEndpointRouteBuilder MapFavoriteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/favorites/{id:guid}/delete", async (Guid id, CurrentUser me, MealFavoriteService favorites) =>
        {
            await favorites.DeleteAsync(me.Id, id);
            return Results.LocalRedirect("/");
        }).RequireAuthorization();

        return app;
    }
}
