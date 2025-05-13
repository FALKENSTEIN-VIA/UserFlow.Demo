namespace UserFlow.API.Data.Entities;

/// <summary>
/// 🔁 Represents a JWT refresh token for session renewal.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// 🔑 Primary key of the token entry.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 🧪 The refresh token string (secure random value).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 📅 UTC timestamp when the token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// ⏳ UTC timestamp when the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 👤 Foreign key reference to the user this token belongs to.
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 🔐 Navigation property to the associated user.
    /// </summary>
    public User? User { get; set; } = null!;
}

/// @remarks
/// Developer Notes:
/// - 🔁 Used to renew expired access tokens without requiring login.
/// - 🕒 Lifetime of the refresh token is limited via `ExpiresAt`.
/// - 👤 Each token is associated with a specific user (nullable for safety).
/// - 🧠 Only store hashed tokens in production if additional security is required.
/// - ⚠️ Always validate that `ExpiresAt` has not passed before issuing a new JWT.
/// - 🔐 Consider revoking all tokens on password change or logout.
