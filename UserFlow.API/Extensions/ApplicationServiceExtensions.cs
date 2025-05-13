/// @file ApplicationServiceExtensions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Extension methods for registering core application services like DbContext, current user service, and authentication.
/// @details
/// This class contains an extension method to configure the application's dependency injection container
/// by adding Entity Framework Core, HttpContextAccessor, CurrentUserService, and AuthService.

using Microsoft.EntityFrameworkCore;
using UserFlow.API.Data;
using UserFlow.API.Services;
using UserFlow.API.Services.Interfaces;

namespace UserFlow.API.Extensions;

/// <summary>
/// 👉 ✨ Provides extension methods for adding core services to the application.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// 👉 ✨ Registers all essential services required by the server application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">The application configuration (used to read connection strings).</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddServerApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        /// 👉 ✨ Register the AppDbContext using PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection"))); // 🔌 Uses "DefaultConnection" from appsettings.json

        /// 👉 ✨ Register HttpContextAccessor to enable access to the current HTTP context
        services.AddHttpContextAccessor(); // 🌐 Allows services to access HttpContext (e.g., for claims)

        /// 👉 ✨ Register a service to access the current user's information
        services.AddScoped<ICurrentUserService, CurrentUserService>(); // 👤 Used for multi-tenancy and auditing

        /// 👉 ✨ Register the authentication service responsible for login, registration, and token management
        services.AddScoped<IAuthService, AuthService>(); // 🔐 Provides token-based authentication services

        return services; // ✅ Return the updated service collection
    }
}

/// @remarks
/// Developer Notes:
/// - 🔌 AppDbContext uses PostgreSQL and retrieves its connection string from `appsettings.json`.
/// - 👤 CurrentUserService provides access to user claims and IDs for multi-tenancy and data filtering.
/// - 🌐 HttpContextAccessor is essential for extracting user claims within scoped services.
/// - 🛠️ This method should be invoked during application startup (typically in `Program.cs`).
/// - 🧱 Add additional scoped or singleton services here if needed to support the backend infrastructure.
