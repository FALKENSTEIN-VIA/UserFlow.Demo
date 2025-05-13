/// @file IdentityServiceExtensions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Extension methods to configure ASP.NET Core Identity and JWT authentication.
/// @details
/// Configures Identity services for managing users, passwords, and sign-ins.
/// Sets up JWT Bearer Authentication using settings from appsettings.json.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserFlow.API.Data;
using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.Settings;

namespace UserFlow.API.Extensions;

/// <summary>
/// 👉 ✨ Provides extension methods for adding Identity and JWT authentication services.
/// </summary>
public static class IdentityServiceExtensions
{
    /// <summary>
    /// 👉 ✨ Configures Identity core services and JWT bearer authentication.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">The application configuration (used to read JWT settings).</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
    {
        /// 👉 ✨ Configure Identity to manage application users
        services.AddIdentityCore<User>(opt =>
        {
            /// 🔓 Relax password complexity for testing environments
            opt.Password.RequireNonAlphanumeric = false;
        })
        .AddRoles<IdentityRole<long>>()                 // 🧑‍💼 Adds support for user roles with long ID
        .AddEntityFrameworkStores<AppDbContext>()       // 💾 Uses EF Core to store Identity data
        .AddSignInManager<SignInManager<User>>()        // 🔐 Enables login-related functionality
        .AddRoleManager<RoleManager<IdentityRole<long>>>();

        /// 👉 ✨ Load JWT settings from configuration
        var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>(); // 📦 Bind config to JwtSettings object

        /// 👉 ✨ Register JWT Bearer Authentication scheme
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true, // ✅ Ensures token has valid signature
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)), // 🔑 Use secure key
                    ValidateIssuer = true,           // 🛡️ Checks if token issuer matches
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,         // 🎯 Validates recipient of token
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,         // ⏰ Rejects expired tokens
                    ClockSkew = TimeSpan.Zero        // ⛔ No tolerance – immediate expiration
                };
#pragma warning restore CS8602
            });

        return services; // ✅ Return updated service collection
    }
}

/// @remarks
/// Developer Notes:
/// - 🔐 Identity setup includes users, roles, and login management with EF Core persistence.
/// - 💡 Password policy is simplified for testing (no non-alphanumeric chars required).
/// - 🧾 JWT Bearer tokens are validated for signature, issuer, audience, and expiration.
/// - ⏰ `ClockSkew = TimeSpan.Zero` disables default 5-min grace period for expiration.
/// - 📄 JWT configuration is loaded from `JwtSettings` section in `appsettings.json`.
/// - 🔑 Keep the signing key secure — never expose it in public or client-side environments.
/// - ⚙️ Should be called early in the startup pipeline (typically in Program.cs).
