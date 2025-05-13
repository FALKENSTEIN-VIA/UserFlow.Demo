/// @file DatabaseConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Configures the database connection for the Web API using Entity Framework Core with PostgreSQL.
/// @details
/// Provides an extension method to register the AppDbContext using a PostgreSQL connection string.
/// Enables optional sensitive data logging for development purposes only.

using Microsoft.EntityFrameworkCore;
using UserFlow.API.Data;

namespace WebAPI.Configurations;

/// <summary>
/// 👉 ✨ Provides a method to configure the database connection for the application.
/// </summary>
public static class DatabaseConfiguration
{
    /// <summary>
    /// 🛠 Configures the AppDbContext using PostgreSQL connection string from configuration.
    /// </summary>
    /// <param name="services">🔧 The DI service collection.</param>
    /// <param name="configuration">📄 The IConfiguration instance containing DB connection strings.</param>
    /// <param name="enableSensitiveLogging">🕵️ Enable sensitive logging (ONLY for development).</param>
    /// <returns>🔁 Returns the modified <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// ❌ Thrown if the 'DefaultConnection' string is missing or empty.
    /// </exception>
    public static IServiceCollection ConfigureDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enableSensitiveLogging = false)
    {
        /// 🔍 Read connection string from appsettings.json
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        /// ❌ Validate that the connection string exists
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("No valid 'DefaultConnection' string found in configuration.");
        }

        /// 🧩 Register AppDbContext with PostgreSQL provider
        services.AddDbContext<AppDbContext>(options =>
        {
            /// 🐘 Use Npgsql provider for PostgreSQL
            options.UseNpgsql(connectionString);

            /// 🕵️ Enable sensitive logging in development only!
            if (enableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging(); // ⚠️ Do NOT use in production

                /// 🧭 Optional: disable tracking for performance/debugging
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        }, ServiceLifetime.Scoped); // 🔄 Scoped lifetime for per-request DbContext

        return services;
    }
}

/// @remarks
/// Developer Notes:
/// - 🔐 Always store connection strings securely (e.g., secrets, env vars).
/// - 🧠 Do NOT enable `EnableSensitiveDataLogging` in production environments.
/// - 🛠️ `QueryTrackingBehavior.NoTracking` is useful for read-heavy scenarios (optional).
/// - 🔄 The DbContext is registered with Scoped lifetime (one per HTTP request).
/// - 📦 Extendable for multi-tenant or dynamic database provider support.
