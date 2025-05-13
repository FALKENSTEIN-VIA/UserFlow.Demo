/// *****************************************************************************************
/// @file HomePage.xaml.cs
/// @brief Code-behind for the HomePage – handles view model binding and page logic.
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// *****************************************************************************************

using UserFlow.Maui.Client.UserControls;
using UserFlow.Maui.Client.ViewModels;

namespace UserFlow.Maui.Client.Views;

/// <summary>
/// 🏠 The main page after successful login. Binds to <see cref="HomeViewModel"/> and
/// provides the user with navigation options such as logout and project access.
/// </summary>
public partial class HomePage : CustomPage
{
    /// <summary>
    /// 🧠 Backing field for the associated view model.
    /// </summary>
    private readonly HomeViewModel _viewModel = null!;

    /// <summary>
    /// 🔧 Constructor that sets up the UI and binds the ViewModel.
    /// </summary>
    /// <param name="viewModel">Injected instance of <see cref="HomeViewModel"/></param>
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
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
/// - This page is loaded after a successful login.
/// - ViewModel is injected via DI and assigned to BindingContext for XAML data binding.
/// - Page layout and actions are defined in HomePage.xaml.
/// *****************************************************************************************
