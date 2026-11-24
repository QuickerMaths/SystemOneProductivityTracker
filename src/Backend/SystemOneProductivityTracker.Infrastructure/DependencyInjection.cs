using Jev.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using SystemOneProductivityTracker.Infrastructure.Persistence;

namespace SystemOneProductivityTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No connection string provided.");

        services.AddDbContext<SystemOneProductivityTrackerDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }),
            optionsLifetime: ServiceLifetime.Singleton);

        services.AddDbContextFactory<SystemOneProductivityTrackerDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));


        services.AddLogging();
        services.AddDataProtection();

        services.AddIdentityCore<IdentityUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
            .AddEntityFrameworkStores<SystemOneProductivityTrackerDbContext>()
            .AddApiEndpoints();

        var apiKey = configuration["TypeSafe:ApiKey"]
            ?? throw new InvalidOperationException("TypeSafe:ApiKey is missing from configuration.");

        services.AddSingleton<ITypeSafeClient>(sp => new TypeSafeClient(new TypeSafeClientOptions
        {
            ApiKey = sp.GetRequiredService<IConfiguration>()["TypeSafe:ApiKey"],
            BaseUrl = "https://openrouter.ai/api",
            LoggerFactory = sp.GetService<ILoggerFactory>(),
        }));

        return services;
    }
}

