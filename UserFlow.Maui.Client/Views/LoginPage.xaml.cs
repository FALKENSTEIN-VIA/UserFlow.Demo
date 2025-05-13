/// *****************************************************************************************
/// @file LoginPage.xaml.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Code-behind der LoginPage. Verbindet XAML mit ViewModel.
/// *****************************************************************************************

using UserFlow.Maui.Client.UserControls;   // 📦 Zugriff auf CustomPage-Basisklasse
using UserFlow.Maui.Client.ViewModels;         // 🧠 Zugriff auf LoginViewModel

namespace UserFlow.Maui.Client.Views;

/// <summary>
/// 📄 Code-behind-Klasse für die LoginPage.
/// Erbt von <c>CustomPage</c> und bindet das <c>LoginViewModel</c> an das UI.
/// </summary>
public partial class LoginPage : CustomPage
{
    private readonly LoginViewModel _viewModel;    // 🧠 ViewModel für Login-Funktionalität

    /// <summary>
    /// 🛠️ Konstruktor, der das ViewModel injiziert und das Binding herstellt.
    /// </summary>
    /// <param name="viewModel">Das ViewModel mit Logik und Bindings.</param>
    public LoginPage(LoginViewModel viewModel)
    {
        // ⚙️ Initialisiert das XAML-Layout
        InitializeComponent();

        // 🔗 Setzt das injizierte ViewModel als BindingContext
        BindingContext = _viewModel = viewModel;

        // ⏳ Async-Task zum Laden der Testdaten
        _viewModel.IsWaitingForTestUsers = _viewModel.TestUsers.Count == 0;
        Task.Run(async () => { await _viewModel.LoadTestUsersWhenAvailableAsync(); });
    }

    /// <summary>
    /// 🔄 Overrides OnAppearing, to call the ViewModel's OnViewAppearingAsync method.
    /// There the ViewModel can subscribe to SignalR ChangeStreams and other events.
    /// </summary>
    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnViewAppearingAsync();
    }

    /// <summary>
    /// 🔄 Overrides OnDisappearing, to call the ViewModel's OnViewDisappearingAsync method.
    /// There the ViewModel can unsubscribe from SignalR ChangeStreams and other events.
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnViewDisappearingAsync().ConfigureAwait(false);
    }
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - Diese Klasse stellt die Verbindung zwischen der XAML-Oberfläche und dem LoginViewModel her.
/// - Sie basiert auf der benutzerdefinierten <c>CustomPage</c>, die Header und BackButton steuert.
/// - Das ViewModel wird über DI injiziert und direkt gebunden.
/// *****************************************************************************************
