/// *****************************************************************************************
/// @file CreateUserPage.xaml.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-09
/// @brief Code-behind file for the CreateUserPage. Binds the ViewModel via constructor injection.
/// *****************************************************************************************

using UserFlow.Maui.Client.ViewModels;

namespace UserFlow.Maui.Client.Views;

/// <summary>
/// 🧾 Page that allows company admins to create a new user account.
/// </summary>
public partial class CreateUserPage : ContentPage
{
    private readonly CreateUserViewModel _viewModel;

    /// <summary>
    /// 🔧 Constructor that receives the ViewModel via DI and binds it.
    /// </summary>
    /// <param name="viewModel">Injected ViewModel with logic for creating users.</param>
    public CreateUserPage(CreateUserViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
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
/// - This page is intended to be accessible by admins only.
/// - ViewModel is injected via DI and contains role-based logic and validation.
/// - Call `App.Services.GetRequiredService<CreateUserPage>()` to navigate here.
/// *****************************************************************************************
