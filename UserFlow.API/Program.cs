/// @file Program.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-02
/// @brief Entry point of the UserFlowAPI application. Configures services, middleware, logging, and Swagger.
/// @details
/// This file sets up the web application host using ASP.NET Core. It configures:
/// - Logging with Serilog
/// - Dependency injection for custom services
/// - Entity Framework and Identity
/// - Exception middleware
/// - Swagger documentation
/// - Authentication and authorization
/// - Development-time seeding and migration

#region 🔧 ✨ Using Directives

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserFlow.API.ChangesStreams.Hubs;
using UserFlow.API.ChangeStreams.Extensions;
using UserFlow.API.Data;
using UserFlow.API.Data.Entities;
using UserFlow.API.Extensions;
using UserFlow.API.Middleware;
using UserFlow.API.Services;

#endregion

/// 👉 ✨ Create WebApplicationBuilder instance
var builder = WebApplication.CreateBuilder(args);

#region 🔧 ✨ Serilog Configuration

/// 👉 ✨ Ensure UTF-8 output in the console
Console.OutputEncoding = System.Text.Encoding.UTF8;

/// 👉 ✨ Configure Serilog logging based on appsettings.json
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration);
});


#endregion

#region 🔧 ✨ Service Configuration

/// 👉 ✨ Add controllers to the DI container
builder.Services.AddControllers();

/// 👉 ✨ Add custom application services (DbContext, CurrentUserService, AuthService, etc.)
builder.Services.AddApplicationServices(builder.Configuration);

/// 👉 ✨ Add ASP.NET Identity and JWT authentication configuration
builder.Services.AddIdentityServices(builder.Configuration);

/// 👉 ✨ Add default authorization policies
builder.Services.AddAuthorization();

/// 👉 ✨ Add Swagger/OpenAPI services for API documentation
builder.Services.AddSwaggerDocumentation();

/// 👉 ✨ Add CORS policy to allow all origins, methods, and headers
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

/// 👉 ✨ TODO: Remove later, only for testing purposes
/// Configure Kestrel to listen on HTTP and HTTPS ports with default certificate
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS with default certificate
    });
    //options.ListenAnyIP(5000);
    //options.ListenAnyIP(5001, listenOptions =>
    //{
    //    listenOptions.UseHttps(); // HTTPS with default certificate
    //});
});



// 💾 Register SignalR Service for real-time communication
// Must be before app.Build() !
builder.Services.AddChangeStreams();

#endregion


/// 👉 ✨ Build the application
var app = builder.Build();

Log.Information("🚀 Building the Application...");
Log.Information("👉 Ensure UTF-8 output in the console...");
Log.Information("👉 Serilog was succesfully configured...");
Log.Information("👉 Controllers have been added to Services...");
Log.Information("👉 AppDbContext, JwtService, CurrentUserService and TestUserStore have been added to Services...");
Log.Information("👉 ASP.NET Identity and JWT Authentication have been added to Services...");
Log.Information("👉 Authorization Policies have been added to Services...");
Log.Information("👉 Swagger/OpenAPI for API documentation has been added to Services...");

#region 🔧 ✨ Middleware Configuration

/// 👉 ✨ Log that application startup has begun
Log.Information("👉 UserFlow API Startup has been initialized...");

/// 👉 ✨ Register global exception handling middleware
app.UseMiddleware<ExceptionsMiddleware>();
Log.Information("👉 Global Exception Handling Middleware has been registered...");

/// 👉 ✨ Enable endpoint routing
app.UseRouting();
Log.Information("👉 Endpoint Routing has been enabled...");

/// 👉 ✨ Enable Swagger middleware (UI + JSON endpoint)
app.UseSwaggerDocumentation();
Log.Information("👉 Swagger Middleware (UI + JSON Endpoint) has been enabled...");

/// 👉 ✨ Enable authentication middleware
app.UseAuthentication();
Log.Information("👉 Authentication Middleware has been enabled...");

/// 👉 ✨ Enable authorization middleware
app.UseAuthorization();

app.Urls.Add("http://+:8080");

/// 👉 ✨ Map controllers to routes (e.g., /api/...)
app.MapControllers();
Log.Information("👉 Controllers have been mapped to Routes...");

/// 👉 ✨ Create a scoped service provider
using var scope = app.Services.CreateScope();
Log.Information("👉 A Scoped Service Provider has been created...");

/// 👉 ✨ Resolve AppDbContext, UserManager and RoleManager from the service provider scope
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();

//Check if the database exists (without migration)
bool databaseWasExisting = dbContext.Database.CanConnect();

//👉 ✨ Apply EF Core database migrations (create or update database schema)
Log.Information("⚠️ Check whether migration needs to be carried out..." + Environment.NewLine);
dbContext.Database.Migrate();

//👉 ✨ Create Triggers for ChangeStreams in PostgreSQL database
var sql = await File.ReadAllTextAsync("Scripts/Triggers/ChangeStreamsTriggers.sql");
await dbContext.Database.ExecuteSqlRawAsync(sql);

///👉 ✨ Get the TestUserStore from the service provider scope
ITestUserStore testUserStore = scope.ServiceProvider.GetRequiredService<ITestUserStore>();

//👉 ✨ Seed only if new migrations were applied
if (app.Environment.IsDevelopment())
{
    if (!databaseWasExisting)
    {
        Log.Information($"🚀 Entity Framework Core database migrations have been applied..." + Environment.NewLine);
        await DataSeeder.SeedAsync(dbContext, roleManager, userManager, testUserStore);
    }
    await DataSeeder.SaveTestUserStore(dbContext, testUserStore);
}

#endregion

#region 👉 ✨ Register a startup log that shows which URLs the API is bound to

/// 👉 ✨ Register a startup log that shows which URLs the API is bound to
app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls.Any()
        ? string.Join(", ", app.Urls)
        : "Standard (z. B. http://localhost:8000 / https://localhost:8001)";

    Log.Information(($"👉 USERFLOW API IS LISTENING ON: {addresses}{Environment.NewLine}"));
});

#endregion

#region 👉 ✨ Register a SignalR ChangeHub for real-time updates


// ✅ Map SignalR-Hub (add no more services afterwards, only Middleware and Mapping)
// Must be after app.Build() !
app.MapHub<ChangeHub>("/changes");

#endregion

/// 👉 ✨ Start the web application
app.Run();


#region 📖 ✨ Remarks

/// @remarks
/// Developer Notes:
/// - 🔧 Startup logic is structured using region blocks for clarity and modularity.
/// - 🛡️ Identity and JWT authentication are fully configured via DI and appsettings.
/// - 🐛 Global exception handling is provided by `ExceptionsMiddleware`.
/// - 🧪 In development, the database is auto-migrated and seeded with test data using `DataSeeder`.
/// - 📖 Swagger is enabled by default and available under `/swagger`.
/// - 🚀 Logging is configured via Serilog and will output both console and rolling file logs.

#endregion
