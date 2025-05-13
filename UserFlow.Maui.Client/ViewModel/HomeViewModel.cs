using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UserFlow.API.HTTP;
using UserFlow.API.HTTP.Base;
using UserFlow.API.HTTP.Services.Interfaces;
using UserFlow.Maui.Client;
using UserFlow.Maui.Client.Views;

namespace UserFlow.Maui.Client.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISecureTokenStore _tokenStore;

    public HomeViewModel(IUnitOfWork unitOfWork, ISecureTokenStore tokenStore, ILogger<HomeViewModel> logger)
        : base(logger)
    {
        _unitOfWork = unitOfWork;
        _tokenStore = tokenStore;
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        await RunApiAsync(
            ExecuteLogoutAsync,
            HandleLogoutSuccessAsync,
            HandleLogoutFailure,
            loadingMessage: "🚪 Logging out...");
    }

    private Task<bool> ExecuteLogoutAsync() =>
        _unitOfWork.Auth.LogoutAsync();

    private async Task HandleLogoutSuccessAsync(bool result)
    {
        await _tokenStore.ClearTokensAsync();
        SecureStorage.Default.Remove("user");

        var loginPage = App.Services.GetRequiredService<LoginPage>();
        await App.Navigator.PushAsync(new NavigationPage(loginPage));

        _logger.LogInformation("✅ User logged out successfully.");
    }

    private void HandleLogoutFailure()
    {
        StatusMessage = "❌ Logout failed.";
        _logger.LogWarning("⚠️ Logout failed via API.");
    }

    [RelayCommand]
    public async Task NavigateCompaniesAsync() =>
        await App.Navigator.PushAsync(new NavigationPage(App.Services.GetRequiredService<CompaniesPage>()));

    [RelayCommand]
    public async Task NavigateUsersAsync() =>
        await App.Navigator.PushAsync(new NavigationPage(App.Services.GetRequiredService<UsersPage>()));

    [RelayCommand]
    public async Task NavigateProjectsAsync() =>
        await App.Navigator.PushAsync(new NavigationPage(App.Services.GetRequiredService<ProjectsPage>()));

    [RelayCommand]
    private void NavigateToRegister()
    {
        // 🔧 Placeholder for registration page navigation
    }
}
