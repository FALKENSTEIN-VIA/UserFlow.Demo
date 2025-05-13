/// @file DesignTimeDbContextFactory.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Provides a factory for creating the AppDbContext at design time.
/// @details
/// This factory allows Entity Framework Core tools (e.g., `dotnet ef migrations`) 
/// to instantiate the DbContext during design-time operations without depending on runtime services.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using UserFlow.API.Services;

namespace UserFlow.API.Data;

/// <summary>
/// 👉 ✨ Factory class for creating an <see cref="AppDbContext"/> at design time.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// 👉 ✨ Creates a new instance of <see cref="AppDbContext"/> for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments passed by tooling (not used here).</param>
    /// <returns>A configured <see cref="AppDbContext"/> instance.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        /// 👉 Build the application configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // 📂 Set the base path to the current directory
            .AddJsonFile("appsettings.json")              // 📄 Load the default app settings file
            .Build();                                     // 🏗️ Build the configuration object

        /// 👉 Setup the DbContext options to use PostgreSQL
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection")); // 🔌 Use the connection string

        /// 👉 Create a simple logger factory for the CurrentUserService dummy instance
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();                        // 📢 Output logs to the console
            builder.SetMinimumLevel(LogLevel.Warning);   // ⚠️ Set minimum log level to Warning
        });

        /// 👉 Create implementations for dependencies needed during design-time
        var designTimeHttpContextAccessor = new HttpContextAccessor(); // 🌐 Dummy accessor (no real HTTP context)
        var designTimeDbContextLogger = loggerFactory.CreateLogger<AppDbContext>(); // 🧾 Logger for DbContext

        /// 👉 Return a new instance of AppDbContext and pass the dependencies
        return new AppDbContext(
            optionsBuilder.Options,                                     // ⚙️ EF Core options
            new CurrentUserService(designTimeHttpContextAccessor),      // 👤 Dummy user context
            designTimeDbContextLogger                                   // 🧾 Logger instance
        );
    }
}

/// @remarks
/// Developer Notes:
/// - 🛠️ This factory is required by EF Core CLI tools (e.g., `dotnet ef migrations add`).
/// - 📄 Loads configuration from `appsettings.json` using the current working directory.
/// - 🧪 Uses minimal dummy services (e.g., HttpContextAccessor) to satisfy constructor dependencies.
/// - 🚫 Do NOT inject actual runtime services — keep the factory self-contained and simple.
/// - 🧾 Logging via `ILogger` is optional but helpful during design-time troubleshooting.
