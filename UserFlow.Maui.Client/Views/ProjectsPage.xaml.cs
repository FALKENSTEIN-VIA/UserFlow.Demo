/// *****************************************************************************************
/// @file ProjectsPage.xaml.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Code-behind file for the ProjectsPage. Automatically loads project data when loaded.
/// *****************************************************************************************

using UserFlow.Maui.Client.UserControls;     // 📦 Custom base class for content pages
using UserFlow.Maui.Client.ViewModels;            // 🧠 ViewModel containing project logic

namespace UserFlow.Maui.Client.Views;

/// <summary>
/// 📄 Code-behind for the "ProjectsPage" view.
/// This page loads the user's projects when it becomes visible.
/// </summary>
public partial class ProjectsPage : CustomPage
{
    private readonly ProjectsViewModel _viewModel;    // 🧠 Reference to the ViewModel for project data

    /// <summary>
    /// 🔧 Constructor that sets up the view and binds the ViewModel.
    /// </summary>
    /// <param name="viewmodel">Injected ViewModel for project logic.</param>
    public ProjectsPage(ProjectsViewModel viewmodel)
    {
        InitializeComponent();
        _viewModel = viewmodel;
        BindingContext = viewmodel;

        // ⏳ Trigger automatic data loading once the page is fully loaded
        Loaded += async (_, _) => await _viewModel.LoadCommand.ExecuteAsync(null);
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
/// - Uses the MVVM pattern and CommunityToolkit.Mvvm for ICommand binding.
/// - Loads project data immediately via the ViewModel when the page appears.
/// - ViewModel must have a public RelayCommand named 'LoadCommand'.
/// - Part of the UserFlow.Maui.Client.Views namespace.
/// *****************************************************************************************
