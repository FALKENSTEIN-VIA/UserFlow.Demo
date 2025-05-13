

/// @file AuthService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Implements authentication logic including JWT and refresh token creation.
/// @details
/// Handles login authentication, token generation, and refresh token scaffolding.
/// Uses ASP.NET Core Identity and JwtSecurityTokenHandler for secure token management.

using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Services;

/// <summary>
/// 👉 ✨ Provides authentication services like JWT and refresh token generation.
/// </summary>
public class AuthService : IAuthService
{
    /// <summary>
    /// 🧱 Identity UserManager instance.
    /// </summary>
    private readonly UserManager<User> _userManager;

    /// <summary>
    /// ⚙️ Application configuration (used to retrieve JWT settings).
    /// </summary>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 👉 ✨ Constructor injecting dependencies.
    /// </summary>
    /// <param name="userManager">UserManager instance for user validation.</param>
    /// <param name="configuration">App configuration for JWT settings.</param>
    public AuthService(UserManager<User> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public async Task<string?> LoginAsync(string email, string password)
    {
        /// 🔍 Try to locate user by email
        var user = await _userManager.FindByEmailAsync(email);

        /// 🔐 Validate password
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            return null!;

        /// 🔑 Return generated JWT token if authentication passes
        return GenerateJwtToken(user);
    }

    /// <summary>
    /// 👉 ✨ Registers a new user (not part of interface, example only).
    /// </summary>
    /// <param name="name">👤 Display name of the user.</param>
    /// <param name="email">📧 Email address.</param>
    /// <param name="password">🔐 Initial password.</param>
    /// <returns>True if user was created successfully.</returns>
    public async Task<bool> RegisterAsync(string name, string email, string password)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            Name = name
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return false;

        /// ℹ️ Example: automatic role assignment (disabled by default)
        /// if (await _roleManager.RoleExistsAsync("User"))
        /// {
        ///     await _userManager.AddToRoleAsync(user, "User");
        /// }

        return true;
    }

    /// <inheritdoc/>
    public async Task<string?> RefreshTokenAsync(string token, string refreshToken)
    {
        /// 🔧 Placeholder: implement refresh token validation logic
        return await Task.FromResult<string?>(null);
    }

    /// <summary>
    /// 🔐 Generates a signed JWT token from user information.
    /// </summary>
    /// <param name="user">Authenticated user entity.</param>
    /// <returns>JWT token string.</returns>
    private string GenerateJwtToken(User user)
    {
        /// 🧾 Define claims to embed in the token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),          // User ID
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),          // Email
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? "")   // Username
        };

        /// 🔐 Create key using appsettings secret
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));

        /// ✍️ Define signing algorithm
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        /// 📦 Construct the token with settings
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        /// 📤 Return token string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// @remarks
/// Developer Notes:
/// - 🛡️ Relies on ASP.NET Core Identity for user verification.
/// - 🔑 JWT tokens include user ID, email, and username.
/// - ⏳ Expiry is set to 1 hour — adjust in appsettings as needed.
/// - 🧪 RefreshTokenAsync is scaffolded but not implemented (requires DB storage/validation).
/// - 📢 For role-based claims, consider adding `new Claim(ClaimTypes.Role, role)` inside claims list.
/// - 🔐 Always store secret keys securely (e.g., Azure Key Vault, environment vars).
