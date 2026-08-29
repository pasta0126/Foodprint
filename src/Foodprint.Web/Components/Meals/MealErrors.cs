using Foodprint.Core.Meals;
using Foodprint.Web.Resources;
using Microsoft.Extensions.Localization;

namespace Foodprint.Web.Components.Meals;

public static class MealErrors
{
    public static string? Describe(MealValidationError error, IStringLocalizer<SharedResource> l)
    {
        var key = error switch
        {
            MealValidationError.None => null,
            MealValidationError.NameRequired => "Meal.Error.NameRequired",
            MealValidationError.NameTooLong => "Meal.Error.NameTooLong",
            MealValidationError.EatenAtTooFarInFuture => "Meal.Error.FutureTime",
            MealValidationError.NotesTooLong => "Meal.Error.NotesTooLong",
            MealValidationError.PortionBothProvided => "Meal.Error.PortionBoth",
            MealValidationError.PortionSizeInvalid => "Meal.Error.PortionSize",
            MealValidationError.PortionGramsOutOfRange => "Meal.Error.PortionGrams",
            MealValidationError.TooManyTags => "Meal.Error.TooManyTags",
            MealValidationError.TagTooLong => "Meal.Error.TagTooLong",
            MealValidationError.UnknownMealGroup => "Meal.Error.UnknownGroup",
            _ => "Meal.Error.Generic",
        };

        return key is null ? null : l[key].Value;
    }
}
