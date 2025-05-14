/// *****************************************************************************************
/// @file CreateUserViewModel.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-09
/// @brief ViewModel for creating new users as an admin from the MAUI client.
/// *****************************************************************************************

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using UserFlow.API.HTTP;
using UserFlow.API.HTTP.Base;
using UserFlow.API.Shared.DTO;

namespace UserFlow.Maui.Client.ViewModels;

/// <summary>
/// 🧑‍💼 ViewModel for admin-controlled user creation via API.
/// </summary>
public partial class CreateUserViewModel : BaseViewModel
{
    #region 🔐 Dependencies

    private readonly IUnitOfWork _unitOfWork;

    #endregion

    #region 🔧 Constructor

    public CreateUserViewModel(IUnitOfWork unitOfWork, ILogger<UsersViewModel> logger) : base(logger)
    {
        _unitOfWork = unitOfWork;
        Initialize();
    }

    #endregion

    #region 🔄 Initialization

    private void Initialize()
    {
        AvailableRoles = ["GlobalAdmin", "Admin", "Manager", "User"];
    }

    #endregion

    #region 📦 Observable Properties

    [ObservableProperty]
    public partial List<string> AvailableRoles { get; set; } = [];

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Role { get; set; } = "User";

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasError { get; set; }

    #endregion

    #region ✅ Create Command

    [RelayCommand]
    public async Task CreateUserAsync()
    {
        await RunApiAsync(
            ExecuteCreateUserAsync,
            HandleCreateUserSuccessAsync,
            HandleCreateUserFailure,
            loadingMessage: "🔄 Creating user...");
    }

    private async Task<UserCreateByAdminDTO?> ExecuteCreateUserAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Role))
            return null;

        var dto = new UserCreateByAdminDTO
        {
            Email = Email,
            Name = Name,
            Role = Role
        };

        var result = await _unitOfWork.Users.CreateByAdminAsync(dto);
        return result ? dto : null;
    }

    private async Task HandleCreateUserSuccessAsync(UserCreateByAdminDTO? dto)
    {
        await App.CurrentPage.DisplayAlert("✅ Success", "User has been created. They can now set their password.", "OK");
        await App.Navigator.PopAsync();
    }

    private void HandleCreateUserFailure()
    {
        StatusMessage = "❌ Failed to create user. Please check the input.";
    }

    #endregion

}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - Clean separation of API Call, Success, Failure using Method Groups
/// - Consistent with RunApiAsync pattern for safe API calls
/// - Client-side validation in ExecuteCreateUserAsync before calling API
/// - Designed with beginner-friendly readability and maintainability
/// *****************************************************************************************
