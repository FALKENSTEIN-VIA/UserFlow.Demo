using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Text.Json;
using UserFlow.API.HTTP;
using UserFlow.API.HTTP.Base;
using UserFlow.API.HTTP.Services.Interfaces;
using UserFlow.API.Shared.DTO;
using UserFlow.Maui.Client;
using UserFlow.Maui.Client.Views;

namespace UserFlow.Maui.Client.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenStore _tokenStorage;

    public static List<UserDTO> LoginTestData { get; set; } = [];

    public LoginViewModel(IUnitOfWork unitOfWork, ISecureTokenStore tokenStorage, ILogger<LoginViewModel> logger)
        : base(logger)
    {
        _unitOfWork = unitOfWork;
        _tokenStorage = tokenStorage;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowAdmins { get; set; } = true;
    partial void OnShowAdminsChanged(bool value) => GetTestUserObservableCollection();

    [ObservableProperty]
    public partial bool ShowUsers { get; set; } = true;
    partial void OnShowUsersChanged(bool value) => GetTestUserObservableCollection();

    public bool CanLoginAsyncExecute() =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand(CanExecute = nameof(CanLoginAsyncExecute))]
    public async Task LoginAsync()
    {
        await RunApiAsync(
            ExecuteLoginAsync,
            HandleLoginSuccessAsync,
            HandleLoginFailure,
            loadingMessage: "🔐 Logging in...");
    }

    private Task<AuthResponseDTO?> ExecuteLoginAsync()
    {
        ErrorMessage = string.Empty;
        var dto = new LoginDTO { Email = Email, Password = Password };
        return _unitOfWork.Auth.LoginAsync(dto);
    }

    private async Task HandleLoginSuccessAsync(AuthResponseDTO? authResult)
    {
        if (authResult?.User == null)
        {
            ErrorMessage = "❌ Login failed. Invalid credentials.";
            return;
        }

        if (authResult.User.NeedsPasswordSetup)
        {
            await App.Navigator.PushAsync(App.Services.GetRequiredService<CompleteRegistrationPage>());
            return;
        }

        var json = JsonSerializer.Serialize(authResult.User);
        await SecureStorage.Default.SetAsync("user", json);
        await App.Navigator.PushAsync(new NavigationPage(App.Services.GetRequiredService<HomePage>()));
    }

    private void HandleLoginFailure()
    {
        ErrorMessage = "❌ Login failed. Server returned no result.";
    }

    [ObservableProperty]
    public partial ObservableCollection<UserDTO> TestUsers { get; set; } = [];

    [ObservableProperty]
    public partial UserDTO? CurrentTestUser { get; set; }

    partial void OnCurrentTestUserChanged(UserDTO? value)
    {
        if (value != null)
        {
            Email = value.Email;
            Password = "Test123!";
        }
    }

    [ObservableProperty]
    public partial bool IsWaitingForTestUsers { get; set; }

    public async Task LoadTestUsersWhenAvailableAsync()
    {
        var timeout = DateTime.Now.AddMinutes(5);

        while (DateTime.Now < timeout)
        {
            try
            {
                var users = await _unitOfWork.Auth.GetTestUsersAsync();
                if (users != null && users.Any())
                {
                    LoginTestData = [.. users];
                    GetTestUserObservableCollection();
                    IsWaitingForTestUsers = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"⚠️ Error: {ex.Message}";
            }

            await Task.Delay(1000);
        }
    }

    public void GetTestUserObservableCollection()
    {
        if (LoginTestData == null) return;

        if (ShowAdmins && ShowUsers)
            TestUsers = new ObservableCollection<UserDTO>(LoginTestData);
        else if (ShowAdmins)
            TestUsers = new ObservableCollection<UserDTO>(LoginTestData.Where(u => u.Role is "GlobalAdmin" or "Admin"));
        else if (ShowUsers)
            TestUsers = new ObservableCollection<UserDTO>(LoginTestData.Where(u => u.Role == "User"));
        else
            TestUsers = [];
    }
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - Uses IUnitOfWork.Auth.LoginAsync() for cleaner architecture
/// - Inherits from BaseViewModel with IsBusy + StatusMessage
/// - Handles SecureStorage for token-free local session
/// - Supports UI filtering of test users by role
/// - Navigation is shell-less via App.Navigator
/// *****************************************************************************************
