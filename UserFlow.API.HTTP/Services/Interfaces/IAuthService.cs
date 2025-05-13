/// *****************************************************************************************
/// @file IAuthService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-09
/// @brief Interface for authentication-related operations with the UserFlow API.
/// @details
/// Defines methods for login, logout, registration, setting passwords, and refreshing tokens.
/// This interface is used by the UserFlow client to communicate with the API authentication endpoints.
/// *****************************************************************************************

using UserFlow.API.Shared.DTO;
using UserFlow.API.Shared.DTO.Auth;

namespace UserFlow.API.Http.Services;

/// <summary>
/// 🔐 Interface defining authentication operations for the UserFlow API.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 🔑 Logs the user in using email and password, and receives JWT and refresh token.
    /// </summary>
    /// <param name="loginDto">Login credentials including email and password.</param>
    /// <returns>AuthResponseDTO with tokens and user info, or null if login fails.</returns>
    Task<AuthResponseDTO?> LoginAsync(LoginDTO loginDto);

    /// <summary>
    /// 🚪 Logs out the current user by clearing stored tokens (client-side only).
    /// </summary>
    /// <returns>True if logout was successful, false otherwise.</returns>
    Task<bool> LogoutAsync();

    /// <summary>
    /// 📝 Registers a new user (via public or admin registration flow).
    /// </summary>
    /// <param name="registerDto">Registration data including email, name and role.</param>
    /// <returns>AuthResponseDTO on success, or null if registration fails.</returns>
    Task<AuthResponseDTO?> RegisterAsync(RegisterDTO registerDto);

    /// <summary>
    /// 🔐 Sets the password for a user who was pre-registered (onboarding flow).
    /// </summary>
    /// <param name="dto">Password setup data including user ID and new password.</param>
    /// <returns>True if password setup succeeded, false otherwise.</returns>
    Task<bool> SetPasswordAsync(SetPasswordDTO dto);

    /// <summary>
    /// 🧩 Completes the registration for a user invited by an admin.
    /// </summary>
    /// <param name="dto">Final registration data such as name, password and token.</param>
    /// <returns>AuthResponseDTO if successful, or null if verification fails.</returns>
    Task<AuthResponseDTO?> CompleteRegistrationAsync(CompleteRegistrationDTO dto);

    /// <summary>
    /// 🔁 Refreshes the access token using a valid refresh token.
    /// </summary>
    /// <param name="dto">Refresh token payload with current refresh token.</param>
    /// <returns>New AuthResponseDTO with updated access and refresh tokens, or null if refresh fails.</returns>
    Task<AuthResponseDTO?> RefreshTokenAsync(RefreshTokenRequestDTO dto);

    /// <summary>
    /// 🔁 Gets a list of test users for testing purposes in the client application.
    /// </summary>
    Task<List<UserDTO>> GetTestUsersAsync();
}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 🔐 Designed for stateless JWT-based authentication flows.
/// - 📦 Returns AuthResponseDTO containing tokens and user info.
/// - 🛡 Supports onboarding flows via CompleteRegistration/SetPassword.
/// - 🧪 Use in combination with ITokenRefreshService for automated token handling.
/// </remarks>
