/// *****************************************************************************************
/// @file MauiProgram.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-09
/// @brief Configures the .NET MAUI app, dependency injection, logging and API integration (rekursionsfrei).
/// *****************************************************************************************

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using UserFlow.API.Http.Auth;
using UserFlow.API.Http.HubServices;
using UserFlow.API.Http.Services;
using UserFlow.API.HTTP;
using UserFlow.API.HTTP.Services;
using UserFlow.API.HTTP.Services.Interfaces;
using UserFlow.Maui.Client.Services;
using UserFlow.Maui.Client.ViewModels;
using UserFlow.Maui.Client.Views;

namespace UserFlow.Maui.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // 👉 Create a new MauiAppBuilder instance
        var builder = MauiApp.CreateBuilder();

        // 👉 Configure fonts
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 🔧 Load configuration from appsettings.json and add it to the builder
        var configuration = LoadConfiguration();
        builder.Configuration.AddConfiguration(configuration);

        // 👉 Add secure storage for token management
        builder.Services.AddScoped<ISecureTokenStore, SecureTokenStore>();

        // 👉 Add HTTP client for API calls
        builder.Services.AddHttpClient("AuthClient", client =>
        {
            client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        // 👉 Add AuthService for authentication
        builder.Services.AddScoped<IAuthService, AuthService>();

        // 👉 Add TokenRefreshService (requires AuthService) 
        builder.Services.AddScoped<ITokenRefreshService>(sp =>
            (ITokenRefreshService)sp.GetRequiredService<IAuthService>());

        // 👉 Add AuthorizedHttpClient for secure API communication with Bearer token handling
        builder.Services.AddScoped<AuthorizedHttpClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 👉 Get TokenRefreshService and SecureTokenStoreService from DI container
            var tokenRefreshService = sp.GetRequiredService<ITokenRefreshService>();
            var tokenStore = sp.GetRequiredService<ISecureTokenStore>();
            var logger = sp.GetRequiredService<ILogger<AuthorizedHttpClient>>();

            // 👉 Create and return an instance of AuthorizedHttpClient with injected dependencies
            return new AuthorizedHttpClient(
                httpClient,
                tokenRefreshService,
                tokenStore,
                logger
            );
        });

        // 👉 Add Services for API endpoints
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<IScreenService, ScreenService>();
        builder.Services.AddScoped<INoteService, NoteService>();
        builder.Services.AddScoped<IEmployeeService, EmployeeService>();
        builder.Services.AddScoped<ICompanyService, CompanyService>();
        builder.Services.AddScoped<IScreenActionService, ScreenActionService>();
        builder.Services.AddScoped<IScreenActionTypeService, ScreenActionTypeService>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 👉 Add SignalR HubService for real-time updates
        builder.Services.AddSingleton<IHubService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config.GetValue<string>("ApiSettings:BaseUrl");
            var hubUrl = $"{baseUrl}/changes";

            // 🚀 Initialize the HubService asynchronously directly here
            var hubService = new HubService(hubUrl);
            _ = hubService.InitializeAsync();

            // 👉 Log successful SignalR connection
            Log.Information("✅ Realtime Updates via SignalR were activated.");

            return hubService;
        });

        // 👉 Add all required ViewModels and Pages 
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<CompaniesViewModel>();
        builder.Services.AddTransient<CompaniesPage>();
        builder.Services.AddTransient<EmployeesViewModel>();
        builder.Services.AddTransient<EmployeesPage>();
        builder.Services.AddTransient<UsersViewModel>();
        builder.Services.AddTransient<UsersPage>();
        builder.Services.AddTransient<CreateUserViewModel>();
        builder.Services.AddTransient<CreateUserPage>();
        builder.Services.AddTransient<ProjectsViewModel>();
        builder.Services.AddTransient<ProjectsPage>();

        /// 👉 ✨ Ensure UTF-8 output in the console
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // 🔧 Configure Serilog for logging
        var logPath = Path.Combine(FileSystem.AppDataDirectory, "Logs/log-.txt");
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // 👉 Add logging service with Serilog
        builder.Logging.AddSerilog(Log.Logger);

        // 👉 Build the app and set the service provider
        var app = builder.Build();

        // 👉 Set the service provider for the app
        App.SetServiceProvider(app.Services);

        // 👉 Return the built app
        return app;
    }

    // 🔧 Load configuration from embedded appsettings.json file
    private static IConfigurationRoot LoadConfiguration()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("UserFlow.Maui.Client.appsettings.json");
        using var reader = new StreamReader(stream!);

        return new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(reader.ReadToEnd())))
            .Build();
    }
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - ✅ Avoids circular dependencies by manually constructing AuthorizedHttpClient.
/// - 🔐 AuthService uses AuthorizedHttpClient and also implements ITokenRefreshService.
/// - 🌍 BaseUrl is platform-aware and read from appsettings.json.
/// - 🧠 Pages and ViewModels are wired up via DI for easy navigation.
/// - 🧩 All API services are scoped for maximum isolation and reuse.
/// *****************************************************************************************
