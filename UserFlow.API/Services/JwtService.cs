/// @file JwtService.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Service to generate JWT tokens for authenticated users.
/// @details
/// Provides methods to create JWT access tokens and refresh tokens.
/// Access tokens include standard claims and roles. Refresh tokens are stored in the database
/// and can be used to issue new JWTs without re-authentication.

using global::UserFlow.API.Data;
using global::UserFlow.API.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace UserFlow.API.Services;

/// <summary>
/// 👉 ✨ Service to generate secure JWT and refresh tokens for authenticated users.
/// </summary>
public class JwtService : IJwtService
{
    /// <summary>
    /// ⚙️ Application configuration for accessing JWT settings.
    /// </summary>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// 🗃️ AppDbContext used to store and manage refresh tokens.
    /// </summary>
    private readonly AppDbContext _context;

    /// <summary>
    /// 🧱 UserManager used to retrieve user roles and manage Identity users.
    /// </summary>
    private readonly UserManager<User> _userManager;

    /// <summary>
    /// 👉 ✨ Constructor injecting configuration, context, and user manager.
    /// </summary>
    public JwtService(IConfiguration configuration, AppDbContext context, UserManager<User> userManager)
    {
        _configuration = configuration;
        _context = context;
        _userManager = userManager;
    }

    /// <inheritdoc/>
    public Task<string> CreateToken(User user) => CreateTokenAsync(user);

    /// <inheritdoc/>
    public async Task<string> CreateTokenAsync(User user)
    {
        /// 🧾 Prepare list of claims for the token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? "")
        };

        /// 🏢 Add CompanyId if available
        if (user.CompanyId.HasValue)
            claims.Add(new Claim("CompanyId", user.CompanyId.Value.ToString()));

        /// 🧑 Add internal identity claims
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, user.Name ?? ""));

        /// 🛡️ Add all role claims for authorization
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        /// 🔐 Create a signing key using the configured secret
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        /// 🧾 Create the token
        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:TokenLifetimeMinutes"]!)),
            signingCredentials: creds
        );

        /// 📤 Return the serialized token
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc/>
    public async Task<string> CreateRefreshTokenAsync(long userId)
    {
        /// 🔁 Generate a secure 64-byte token string
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        /// 🧾 Build refresh token entity
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        /// 🧹 Remove old tokens before saving the new one
        var oldTokens = _context.RefreshTokens.Where(x => x.UserId == userId);
        _context.RefreshTokens.RemoveRange(oldTokens);

        /// 💾 Save the new refresh token
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return token;
    }
}

/// @remarks
/// Developer Notes:
/// - 🔐 JWT tokens contain standard claims (sub, email, username) and app-specific claims (CompanyId, roles).
/// - ♻️ Refresh tokens are stored in the DB and valid for 7 days.
/// - ✅ Existing refresh tokens are removed before a new one is saved to avoid reuse.
/// - 🧠 Secret keys and expiration times are configured in `JwtSettings` (from `appsettings.json`).
/// - 🔧 This service is the backbone of all secure authentication and should be kept stable and audited.

