/// @file SwaggerExtensions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Extension methods to configure Swagger/OpenAPI documentation for UserFlowAPI.
/// @details
/// Provides clean setup methods to integrate Swagger UI and OpenAPI specification,
/// including JWT authentication support directly within the Swagger UI.

using AspNetCore.Swagger.Themes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace UserFlow.API.Extensions;

/// <summary>
/// 👉 ✨ Provides extension methods for configuring Swagger services and middleware.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// 👉 ✨ Registers Swagger/OpenAPI services including JWT security schemes.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        /// 🧩 Enable minimal API endpoint discovery
        services.AddEndpointsApiExplorer();

        /// 📄 Register Swagger generator and define metadata
        services.AddSwaggerGen(c =>
        {
            /// 📘 Define OpenAPI document (version, title, contact, license)
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "UserFlow API",
                Version = "v1",
                Description = "Backend for UserFlow Clients",
                Contact = new OpenApiContact
                {
                    Name = "VIA Software GmbH",
                    Email = "info@viasoftware.de",
                    Url = new Uri("https://www.via-software.com/de/")
                },
                License = new OpenApiLicense
                {
                    Name = "All rights reserved",
                    Url = new Uri("https://www.via-software.com/de/")
                }
            });

            /// 🔐 Add JWT Bearer authentication scheme to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: 'Authorization: Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            /// 🔐 Enforce JWT scheme as a global requirement for API access
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services; // ✅ Return modified service collection
    }

    /// <summary>
    /// 👉 ✨ Enables Swagger middleware for serving the generated OpenAPI specification and Swagger UI.
    /// </summary>
    /// <param name="app">The application builder to configure middleware.</param>
    /// <returns>The updated <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        /// 🌐 Enable OpenAPI endpoint (swagger.json)
        app.UseSwagger();

        /// 🌈 Customize Swagger UI
        app.UseSwaggerUI(ModernStyle.Light, c =>
        {
            /// 📘 Configure Swagger endpoint and UI settings
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserFlow API v1");
            c.RoutePrefix = "swagger"; // 🌍 Swagger UI available at /swagger
            c.DocumentTitle = "UserFlow API Dokumentation";
            c.OAuthAppName("UserFlowAPI Swagger UI");

            /// 🧭 Sort tags and operations alphabetically
            c.ConfigObject.AdditionalItems["tagsSorter"] = "alpha";
            c.ConfigObject.AdditionalItems["operationsSorter"] = "method";

            /// 📂 Collapse all endpoints by default
            c.DocExpansion(DocExpansion.None);
        });

        return app; // ✅ Return modified app pipeline
    }
}

/// @remarks
/// Developer Notes:
/// - 📘 Adds clean Swagger UI with full API metadata and JWT security integration.
/// - 🔐 Allows testing secured endpoints directly in Swagger UI by entering a valid JWT token.
/// - 🌍 Swagger UI is accessible under `/swagger` by default (customizable).
/// - 🧭 Operations and tags are sorted alphabetically for clarity.
/// - ⚙️ Use `ModernStyle.Light` from AspNetCore.Swagger.Themes for a polished UI experience.
/// - 🧼 Always disable or protect Swagger in production environments if sensitive endpoints exist.
