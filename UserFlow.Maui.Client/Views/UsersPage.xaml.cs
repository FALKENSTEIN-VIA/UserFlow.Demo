/// *****************************************************************************************
/// @file UsersPage.xaml.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-12
/// @brief Code-behind for UsersPage using CustomPage. Manages ViewModel lifecycle, data load and user updates.
/// *****************************************************************************************

using System.ComponentModel;
using UserFlow.API.Shared.DTO;
using UserFlow.Maui.Client.UserControls;
using UserFlow.Maui.Client.ViewModels;

namespace UserFlow.Maui.Client.Views;

/// <summary>
/// 👥 UsersPage displaying the user list with ChangeStreams integration.
/// </summary>
public partial class UsersPage : CustomPage
{
    /// <summary>
    /// 🧠 Backing field for the associated view model.
    /// </summary>
    private readonly UsersViewModel _viewModel;

    #region 🔧 Constructor

    public UsersPage(UsersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;            // 💾 Store ViewModel
        BindingContext = _viewModel;       // 🔗 Set binding context

        // ⏳ Trigger automatic data loading once the page is fully loaded
        Loaded += async (_, _) =>
        {
            await _viewModel.LoadCommand.ExecuteAsync(null);

            // 💡 Optionally scroll to CurrentItem after load
            _viewModel.ScrollToCurrentUserCommand.Execute(null);
        };

        // 💡 ScrollToItem Beobachten
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    #endregion


    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.ScrollToUser) && _viewModel.ScrollToUser is not null)
        {
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(150);

                if (UsersCollectionView.ItemsSource is IEnumerable<UserDTO> items
                    && items.Contains(_viewModel.ScrollToUser))
                {
                    UsersCollectionView.ScrollTo(0, animate: false);
                    UsersCollectionView.ScrollTo(_viewModel.ScrollToUser, position: ScrollToPosition.Center, animate: true);
                }
            });
        }
    }


    #region 📅 View Lifecycle Handling

    /// <summary>
    /// 🔄 OnAppearing triggers the ViewModel's ChangeStreams subscription.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnViewAppearingAsync();
    }

    /// <summary>
    /// 🚪 OnDisappearing triggers the ViewModel's ChangeStreams unsubscription.
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.OnViewDisappearingAsync();
    }

    #endregion

    #region 🖱 UI Test Button (Update User)

    /// <summary>
    /// 🔄 Button click handler to test user update and ChangeStreams reaction.
    /// </summary>
    private async void Button_Clicked(object sender, EventArgs e)
    {
        await _viewModel.UpdateCurrentUserAsync();
    }

    #endregion
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - Uses CustomPage.
/// - Handles ViewModel lifecycle (subscribe/unsubscribe to ChangeStreams).
/// - On Loaded triggers LoadCommand & optional ScrollToCurrentUserCommand.
/// - Includes test button for manual user update.
/// *****************************************************************************************
