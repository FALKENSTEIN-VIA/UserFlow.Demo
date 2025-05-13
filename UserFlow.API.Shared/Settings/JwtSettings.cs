/// *****************************************************************************************
/// @file JwtSettings.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Settings class for JWT-related configuration (key, issuer, audience, token lifespan).
/// *****************************************************************************************

namespace UserFlow.API.Shared.Settings
{
    /// <summary>
    /// 🔐✨ Represents JWT configuration settings used for token generation and validation.
    /// </summary>
    /// <remarks>
    /// This class binds values from the `JwtSettings` section in `appsettings.json`.
    /// It contains all required parameters to configure JWT-based authentication, including
    /// the secret key, issuer, audience, and token expiration intervals.
    /// </remarks>
    public class JwtSettings
    {
        /// <summary>
        /// 🔑 Secret key used to sign the JWT access and refresh tokens.
        /// </summary>
        /// <remarks>
        /// This should be a long and random base64-encoded string.
        /// Changing this value will invalidate all previously issued tokens.
        /// </remarks>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 🏢 Issuer of the JWT token (e.g., "UserFlowAPI").
        /// </summary>
        /// <remarks>
        /// This is used in the `iss` claim and should match the value expected by clients.
        /// </remarks>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// 🧑‍💻 Audience for the JWT token (e.g., "UserFlowClient").
        /// </summary>
        /// <remarks>
        /// This is used in the `aud` claim to specify the intended recipients of the token.
        /// </remarks>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// ⏱️ Expiration time (in minutes) for access tokens.
        /// </summary>
        /// <remarks>
        /// Typical values are between 15 and 120 minutes depending on security needs.
        /// </remarks>
        public int AccessTokenExpirationMinutes { get; set; }

        /// <summary>
        /// 🔁 Expiration time (in days) for refresh tokens.
        /// </summary>
        /// <remarks>
        /// Defines how long a refresh token remains valid (e.g., 7 or 14 days).
        /// </remarks>
        public int RefreshTokenExpirationDays { get; set; }
    }
}

/// @remarks
/// 🛠️ **Developer Notes**
/// 
/// - This class is used by the `JwtService` and `AuthService` for generating, signing, and validating tokens.
/// - All values are typically injected via `IOptions<JwtSettings>` or `IOptionsMonitor<JwtSettings>`.
/// 
/// ✅ **Example Configuration in appsettings.json**
/// ```json
/// "JwtSettings": {
///   "Key": "...",
///   "Issuer": "UserFlowAPI",
///   "Audience": "UserFlowClient",
///   "AccessTokenExpirationMinutes": 60,
///   "RefreshTokenExpirationDays": 7
/// }
/// ```
/// 
/// 📁 **Related Files**
/// - `AppSettings.cs` – Root wrapper class for all config sections
/// - `Program.cs` – Where services are configured using `builder.Services.Configure<JwtSettings>(...)`
/// - `JwtService.cs` – Uses these values to create tokens
/// 
/// 📌 **Security Advice**
/// - Store the secret key securely (e.g., in Azure Key Vault or environment variables).
/// - Never check sensitive values like the `Key` into version control.
/// 
/// *****************************************************************************************
