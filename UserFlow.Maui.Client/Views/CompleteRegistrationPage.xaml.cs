/// *****************************************************************************************
/// @file CompleteRegistrationViewModel.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief ViewModel for completing registration of pre-registered users by setting a password.
/// *****************************************************************************************

namespace UserFlow.Maui.Client.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;   // 🧰 MVVM Toolkit base class
using CommunityToolkit.Mvvm.Input;            // 🧠 RelayCommand support
using System.Threading.Tasks;                 // 🕒 Async support
using UserFlow.API.HTTP;
using UserFlow.API.Shared.DTO.Auth;           // 📄 DTOs for registration
using UserFlow.Maui.Client.Views;             // 📱 UI navigation targets

/// <summary>
/// 🧠 ViewModel for completing registration of pre-registered users by entering a password.
/// </summary>
public partial class CompleteRegistrationViewModel : ObservableObject
{
    private readonly AuthorizedHttpClient _httpClient; // 🌐 HTTP client injected via DI

    /// <summary>
    /// 👉 Constructor injecting HttpClient for API communication.
    /// </summary>
    public CompleteRegistrationViewModel(AuthorizedHttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// 📧 Email address used for registration.
    /// </summary>
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🔑 Password the user wants to set.
    /// </summary>
    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    /// <summary>
    /// ✅ Command to submit the registration and navigate to the homepage.
    /// </summary>
    [RelayCommand]
    private async Task CompleteRegistrationAsync()
    {
        // 📝 Create DTO from current state
        var dto = new CompleteRegistrationDTO
        {
            Email = Email,
            Password = Password
        };

        // 📡 Send request to backend API
        var response = await _httpClient.PostAsync("api/auth/complete-registration", dto);


        if (response.IsSuccessStatusCode)
        {
            // ✅ Success: Navigate to HomePage
            await App.Navigator.PushAsync(App.Services.GetRequiredService<HomePage>());
        }
        else
        {
            // ⚠️ Failure: Show error message from backend
            var error = await response.Content.ReadAsStringAsync();
            await App.CurrentPage.DisplayAlert("Fehler", error, "OK");
        }
    }
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - Used during the first login of a pre-created user account.
/// - Sends email and password to complete the setup.
/// - Navigates to HomePage on success.
/// *****************************************************************************************
