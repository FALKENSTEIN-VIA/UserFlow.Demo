/// @file ServiceCollectionExtensions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Provides extension methods to configure application-specific services like settings binding and database context.
/// @details
/// This class handles dependency injection registration for JWT settings, core services, and the database context.
/// It ensures that required services are correctly configured and available throughout the application.

using Microsoft.EntityFrameworkCore;
using UserFlow.API.Data;
using UserFlow.API.Services;
using UserFlow.API.Services.Interfaces;
using UserFlow.API.Shared.Settings;

namespace UserFlow.API.Extensions;

/// <summary>
/// 👉 ✨ Provides extension methods to register core application services into the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 👉 ✨ Configures core services including settings binding, JWT services, CurrentUserService, and the database context.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration used for binding settings.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        /// 🛠️ Bind JWT settings section from appsettings.json
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings")); // 🔐 Strongly-typed config binding

        /// 🔐 Register service for handling JWT creation and validation
        services.AddScoped<IJwtService, JwtService>(); // 🧾 Token generation and validation logic

        /// 👤 Register the service to access the current authenticated user's context
        services.AddScoped<ICurrentUserService, CurrentUserService>(); // 📇 Used for user context (claims, CompanyId, etc.)

        /// 👤 Register the service to manage test users data for the client (Test purpose only)
        services.AddSingleton<ITestUserStore, TestUserStore>();

        /// 👉 ✨ Add user service for identity user creation and management
        services.AddScoped<IUserService, UserService>();

        /// 💾 Register the EF Core DbContext with dynamic config resolution
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            /// 📦 Resolve IConfiguration from the DI container
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            /// 🔌 Connect to PostgreSQL using the connection string from appsettings.json
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"));
        });

        /// ✅ Return the updated service collection
        return services;
    }
}

/// @remarks
/// Developer Notes:
/// - 🧩 Centralizes all backend service registrations to keep `Program.cs` clean and maintainable.
/// - 🔐 JwtSettings are bound once and injectable via `IOptions<JwtSettings>`.
/// - 👤 CurrentUserService uses `IHttpContextAccessor` to extract user context and claims.
/// - 🗄️ AppDbContext is registered as scoped and reads connection settings dynamically at runtime.
/// - ⚠️ Ensure strong secrets and production-safe JWT settings (e.g., key length, lifetime) are configured properly.
