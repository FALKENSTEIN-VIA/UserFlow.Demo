/// @file SwaggerConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Configures Swagger for API documentation and JWT authentication integration.
/// @details
/// Provides an extension method to register and configure Swagger/OpenAPI documentation
/// including JWT Bearer Token support for secured endpoints.

using Microsoft.OpenApi.Models;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Provides a method to configure Swagger for the API documentation.
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// 📚 Configures Swagger generation and JWT Bearer integration.
    /// </summary>
    /// <param name="services">🔧 The service collection to configure.</param>
    /// <returns>🔁 The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        /// 🧪 Add Swagger generator service
        services.AddSwaggerGen(c =>
        {
            /// 📄 Define the Swagger/OpenAPI doc
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "UserFlow API",                       // 🏷 API title
                Version = "v1",                               // 🔢 API version
                Description = "API documentation for the UserFlow API" // 📝 Description
            });

            /// 🔐 Define JWT Bearer scheme (header-based API key)
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",                      // 🪪 Header name
                Description = "Enter the Bearer token: 'Bearer {token}'", // ℹ️ Instruction
                In = ParameterLocation.Header,               // 📍 In HTTP header
                Type = SecuritySchemeType.ApiKey,            // 🔑 Type: API Key
                Scheme = "Bearer"                            // ⚙️ Scheme name
            };

            /// 📝 Register security scheme
            c.AddSecurityDefinition("Bearer", securityScheme);

            /// 🔐 Require Bearer token globally
            var securityRequirement = new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }    // ✅ No scopes required
            };

            c.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }
}

/// @remarks
/// Developer Notes:
/// - 📘 Swagger provides interactive API documentation for developers and testers.
/// - 🔐 JWT Bearer token integration allows secured endpoints to be tested via Swagger UI.
/// - ⚠️ Only enable Swagger in Development or Staging — avoid exposing internal API details in production.
/// - 🧩 Easily extendable for OAuth2 flows, versioning, or custom UI enhancements.
