/// @file AuthConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Configures authentication and authorization services for the Web API.
/// @details
/// # AuthConfiguration
/// 
/// ## Main Functions
/// - Configures JWT authentication based on settings from `appsettings.json`.
/// - Adds standard authorization services.
/// 
/// ## Used Technologies
/// - JWT (JSON Web Token) for authentication.
/// - Microsoft IdentityModel for token validation.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace WebAPI.Configurations;

/// <summary>
/// 👉 ✨ Provides extension methods to configure authentication and authorization services.
/// </summary>
public static class AuthConfiguration
{
    /// <summary>
    /// 🛡️ Configures JWT authentication based on 'JwtSettings' in appsettings.json.
    /// </summary>
    /// <param name="services">🔧 The DI service collection.</param>
    /// <param name="configuration">📄 The app configuration containing JWT settings.</param>
    /// <returns>🔁 The updated <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// ❌ Thrown if required configuration values are missing.
    /// </exception>
    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        /// 🔍 Get the JwtSettings section from configuration
        var jwtSection = configuration.GetSection("JwtSettings");

        /// ⚠️ Check if section exists
        if (!jwtSection.Exists())
            throw new InvalidOperationException("Configuration section 'JwtSettings' is missing. Make sure appsettings.json has a 'JwtSettings' section.");

        /// 📥 Extract key values
        var key = jwtSection.GetValue<string>("Key");
        var issuer = jwtSection.GetValue<string>("Issuer");
        var audience = jwtSection.GetValue<string>("Audience");

        /// ❌ Validate Key
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT Key ('JwtSettings:Key') is not configured.");

        /// ❌ Validate Issuer
        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("JWT Issuer ('JwtSettings:Issuer') is not configured.");

        /// ❌ Validate Audience
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT Audience ('JwtSettings:Audience') is not configured.");

        /// 🔐 Create the symmetric security key
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        /// 🔐 Register authentication services
        services.AddAuthentication(options =>
        {
            /// 🔑 Use JWT Bearer as default
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            /// ⚙️ Configure token validation parameters
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,                    // ✅ Ensure token has correct issuer
                ValidateAudience = true,                  // ✅ Ensure token is for the correct audience
                ValidateLifetime = true,                  // ✅ Check expiration
                ValidateIssuerSigningKey = true,          // ✅ Validate the token's signature
                ValidIssuer = issuer,                     // 📄 Value from config
                ValidAudience = audience,                 // 📄 Value from config
                IssuerSigningKey = signingKey,            // 🔐 Signing key
                ClockSkew = TimeSpan.Zero                 // ⏱ No grace period
            };
        });

        return services;
    }

    /// <summary>
    /// 🛂 Configures default authorization services.
    /// </summary>
    /// <param name="services">🔧 The DI service collection.</param>
    /// <returns>🔁 The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection ConfigureAuthorization(this IServiceCollection services)
    {
        /// ✅ Add default policy-based authorization
        services.AddAuthorization();
        return services;
    }
}

/// @remarks
/// Developer Notes:
/// - 🔐 JWT-based authentication uses symmetric key defined in appsettings.json.
/// - ⚠️ Always define 'Key', 'Issuer' and 'Audience' securely and consistently.
/// - ⏱ ClockSkew is 0, which disables token grace period on expiration.
/// - 🧱 Extendable: You can add custom role-based or claims-based policies here.
/// - 🔍 Validate tokens carefully in production with HTTPS and secure secrets.
