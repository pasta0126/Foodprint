using Foodprint.Core.Auth;
using Foodprint.Core.Data;
using Foodprint.Core.Meals;
using Foodprint.Core.Profiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Foodprint.Core;

public static class FoodprintServiceCollectionExtensions
{
    /// <summary>Registers the data layer plus every Core service, for both the web app and the CLI.</summary>
    public static IServiceCollection AddFoodprintCore(
        this IServiceCollection services, string connectionString, IConfiguration configuration)
    {
        services.AddFoodprintData(connectionString);
        services.Configure<FoodprintOptions>(configuration.GetSection(FoodprintOptions.SectionName));

        if (services.All(d => d.ServiceType != typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }

        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IAttemptLimiter, InMemoryAttemptLimiter>();

        services.AddScoped<RegistrationService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AdminBootstrapper>();
        services.AddScoped<ProfileService>();
        services.AddScoped<MealGroupService>();
        services.AddScoped<MealEntryService>();
        services.AddScoped<MealFavoriteService>();
        services.AddScoped<DiaryService>();
        services.AddScoped<SummaryService>();

        return services;
    }
}
