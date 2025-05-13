// *****************************************************************************************
// @file App.xaml.cs
// @author Claus Falkenstein
// @company VIA Software GmbH
// @date 2025-04-27
// @brief Root class for the MAUI application with custom window and navigation logic.
// *****************************************************************************************

using UserFlow.API.HTTP.Services.Interfaces;
using UserFlow.Maui.Client.Views;

namespace UserFlow.Maui.Client;

/// <summary>
/// 📱 Application entry point with window and session-based navigation setup.
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Application.Current!.UserAppTheme = AppTheme.Dark;
    }

    /// <summary>
    /// 🪟 Gets the current main window.
    /// </summary>
    public static Window MainWindow =>
        Current?.Windows.FirstOrDefault()
        ?? throw new InvalidOperationException("No window found.");

    /// <summary>
    /// 📄 Gets the current root page of the app.
    /// </summary>
    public static Page CurrentPage =>
        MainWindow.Page
        ?? throw new InvalidOperationException("No page set on main window.");

    /// <summary>
    /// 🧭 Provides access to the Navigation stack.
    /// </summary>
    public static INavigation Navigator =>
        (CurrentPage as NavigationPage)?.Navigation
        ?? throw new InvalidOperationException("Navigation not available.");

    /// <summary>
    /// 🔄 Replaces the current root page of the app.
    /// </summary>
    public static void SetRootPage(Page page) => MainWindow.Page = page;

    /// <summary>
    /// 🧩 Holds the globally available service provider.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// 🔧 Assigns the service provider after DI is built.
    /// </summary>
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        Services = serviceProvider;
    }

    /// <summary>
    /// 🪟 Creates the initial application window.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window();
        InitializeWindow(window);

        // ⚠️ Async-Check mit Loading-Page
        window.Page = new NavigationPage(new LoadingPage());


        // Async-Initialisierung starten
        Task.Run(async () =>
        {
            var isLoggedIn = await IsLoggedInAsync();
            Page startPage = isLoggedIn
                ? Services.GetRequiredService<HomePage>()
                : Services.GetRequiredService<LoginPage>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                window.Page = new NavigationPage(startPage);
            });
        });

        return window;
    }


    /// <summary>
    /// 🛠 Sets the window dimensions and positions it in the center of the screen.
    /// </summary>
    public static void InitializeWindow(Window window)
    {
        const int windowWidth = 800;
        const int windowHeight = 770;

        window.Width = windowWidth;
        window.Height = windowHeight;

        var displayInfo = DeviceDisplay.MainDisplayInfo;
        var screenWidth = displayInfo.Width / displayInfo.Density;
        var screenHeight = displayInfo.Height / displayInfo.Density;

        window.X = (screenWidth - windowWidth) / 2;
        window.Y = (screenHeight - windowHeight) / 2;
    }

    /// <summary>
    /// 🔐 Checks whether a valid login token exists in secure storage.
    /// </summary>
    private async Task<bool> IsLoggedInAsync()
    {
        var tokenStore = Services.GetRequiredService<ISecureTokenStore>();
        var token = await tokenStore.GetAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    //public static async Task<bool> IsLoggedInAsync()
    //{
    //    try
    //    {
    //        var tokenService = Services.GetRequiredService<ISecureTokenStore>();
    //        var token = await tokenService.GetAccessTokenAsync();

    //        if (string.IsNullOrWhiteSpace(token))
    //            return false;

    //        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
    //        var jwt = handler.ReadJwtToken(token);

    //        return jwt.ValidTo > DateTime.UtcNow;
    //    }
    //    catch
    //    {
    //        return false;
    //    }
    //}
}

/// *****************************************************************************************
// @remarks 📄 Developer Notes:
// - Creates a centered, fixed-size window ideal for desktop scenarios.
// - Use SetRootPage or Navigator to manually control app flow.
// - Default startup logic always navigates to LoginPage.
// - Add login/session check later to support automatic redirection to HomePage.
// - Services must be registered before navigation logic executes.
// *****************************************************************************************
