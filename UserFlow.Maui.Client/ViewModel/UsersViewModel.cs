using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using UserFlow.API.ChangeStreams.Helper;
using UserFlow.API.Http.Base;
using UserFlow.API.Http.HubServices;
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO;
using UserFlow.API.Shared.Notifications;
using UserFlow.Maui.Client;

namespace UserFlow.Maui.Client.ViewModels;

public partial class UsersViewModel : BaseChangeStreamsViewModel
{
    private readonly IUnitOfWork _unitOfWork;

    public UsersViewModel(IUnitOfWork unitOfWork, IHubService hubService, ILogger<UsersViewModel> logger)
        : base(hubService, logger)
    {
        _unitOfWork = unitOfWork;
    }

    protected override string ChangeStreamEntityName => "Users";

    [ObservableProperty]
    public partial ObservableCollection<UserDTO> Users { get; set; } = [];

    [ObservableProperty]
    public partial UserDTO? CurrentUser { get; set; }

    [ObservableProperty]
    public partial UserDTO? ScrollToUser { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunApiAsync(
            () => _unitOfWork.Users.GetAllUsersAsync(includeCompany: false),
            result =>
            {
                Users = new ObservableCollection<UserDTO>((result ?? []).OrderBy(x => x.Id));
                CurrentUser = Users.FirstOrDefault(x => x.Id == CurrentUser?.Id) ?? Users.FirstOrDefault();
                ScrollToUser = CurrentUser;
                _logger.LogInformation("✅ Loaded {Count} users.", Users.Count);
                return Task.CompletedTask;
            },
            onFailure: () => _logger.LogWarning("⚠️ Failed to load users."),
            loadingMessage: "👥 Loading users...");
    }

    [RelayCommand]
    public async Task UpdateCurrentUserAsync()
    {
        if (CurrentUser == null) return;

        await RunApiAsync(
            async () =>
            {
                var updateDto = new UpdateUserDTO
                {
                    Id = CurrentUser.Id,
                    Name = $"{CurrentUser.Name} (Updated {DateTime.Now:T})",
                    Email = CurrentUser.Email,
                };
                return await _unitOfWork.Users.UpdateUserAsync(updateDto) ? updateDto : null;
            },
            async updateDto =>
            {
                if (updateDto == null)
                {
                    StatusMessage = "❌ Failed to update user (null response).";
                    return;
                }

                StatusMessage = $"✅ User '{updateDto.Name}' updated successfully.";
                await LoadAsync();
            },
            onFailure: () => StatusMessage = "❌ Failed to update user.",
            loadingMessage: "🔄 Updating user...");
    }

    [RelayCommand]
    public void ScrollToCurrentUser()
    {
        if (CurrentUser != null)
        {
            ScrollToUser = CurrentUser;
            _logger.LogInformation("📍 ScrollTo triggered for User Id {Id}", CurrentUser.Id);
        }
    }

    protected override void OnChangeNotificationReceived(ChangeNotification notification)
    {
        // INFO: Enable this to reload the entire collection on change notification.
        //await LoadAsync();

        // INFO: Here the collection is updated based on the change notification.
        CollectionChangeHandler.ApplyChange(
            Users,
            notification,
            id => _unitOfWork.Users.GetUserByIdAsync(id),
            user => user.Id,
            action => App.Current?.Dispatcher.Dispatch(action)
        );
        ScrollToCurrentUser();
    }
}
