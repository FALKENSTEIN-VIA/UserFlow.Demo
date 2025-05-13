/// *****************************************************************************************
/// @file AppSettings.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Root settings class to bind configuration sections like JwtSettings from configuration files.
/// *****************************************************************************************

namespace UserFlow.API.Shared.Settings
{
    /// <summary>
    /// 👉 ✨ Aggregates application-wide configuration into a single strongly typed object.
    /// </summary>
    /// <remarks>
    /// This class serves as the root for binding configuration sections from `appsettings.json`.
    /// Each property maps to a configuration section such as `JwtSettings`.
    /// </remarks>
    public class AppSettings
    {
        /// <summary>
        /// 🔐 Settings related to JWT authentication (e.g., key, issuer, audience, expiry).
        /// </summary>
        public JwtSettings JwtSettings { get; set; } = new();
    }
}

/// @remarks
/// 🛠️ **Developer Notes**
/// 
/// - This class binds top-level configuration from `appsettings.json` into a structured object.
/// - Used for dependency injection of `IOptions<AppSettings>` or `IOptionsMonitor<AppSettings>`.
/// - Easily extendable by adding new properties:
///   - For example: `public EmailSettings Email { get; set; } = new();`
///   - Or nested sections like `LoggingSettings`, `ApiSettings`, etc.
/// 
/// ✅ **Example Configuration**
/// ```json
/// "JwtSettings": {
///   "Key": "...",
///   "Issuer": "...",
///   "Audience": "...",
///   "ExpiryMinutes": 60
/// }
/// ```
/// 
/// 📁 **Related Files**
/// - `JwtSettings.cs`: Defines the structure of the JWT-specific settings.
/// - `Program.cs`: Uses `builder.Services.Configure<AppSettings>(...)` to bind configuration.
/// 
/// 📌 **Tip**
/// - For consistency, keep all settings classes in the `UserFlow.API.Shared.Settings` namespace.
/// - Group related settings logically and document clearly for maintainability.
/// 
// *****************************************************************************************
