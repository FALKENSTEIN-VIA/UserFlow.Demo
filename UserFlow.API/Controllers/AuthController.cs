/// @file AuthController.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Provides authentication endpoints for login and registration.
/// @details
/// This controller handles user authentication flows including:
/// - Login and logout,
/// - Admin-triggered registration,
/// - Password setup and registration completion,
/// - Token issuing and refresh,
/// - Test user access (in dev mode only).
/// Uses Identity, JWT and refresh token infrastructure.
///
/// @endpoints
/// - POST /api/auth/login                   → Logs in a user and issues a JWT
/// - POST /api/auth/logout                  → Logs a user out (log only, no state)
/// - POST /api/auth/register                → Registers a new user (Admin only)
/// - POST /api/auth/set-password            → Sets the password and activates a user
/// - POST /api/auth/complete-registration   → Completes a registration for a user without email/password
/// - POST /api/auth/refresh                 → Refreshes access token using a valid refresh token
/// - GET  /api/auth/testusers               → Returns test users (only in dev/testing)

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserFlow.API.Data;
using UserFlow.API.Data.Entities;
using UserFlow.API.Helpers;
using UserFlow.API.Services;
using UserFlow.API.Shared.DTO;
using UserFlow.API.Shared.DTO.Auth;

namespace UserFlow.API.Controllers;

/// <summary>
/// 🔐 Handles authentication logic for login, registration, token refresh, etc.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    #region 🔒 Fields

    /// <summary>📦 Database context</summary>
    private readonly AppDbContext _context;

    /// <summary>🔐 JWT service for token generation</summary>
    private readonly IJwtService _jwtService;

    /// <summary>👤 Sign-in manager (Identity)</summary>
    private readonly SignInManager<User> _signInManager;

    /// <summary>📝 Logger instance</summary>
    private readonly ILogger<AuthController> _logger;

    /// <summary>👤 Test user store for demo purposes</summary>
    private readonly ITestUserStore _testUserStore;

    #endregion

    #region 🏗️ Constructor

    /// <summary>
    /// 🧱 Constructor injecting required services.
    /// </summary>
    public AuthController(
        AppDbContext context,
        IJwtService jwtService,
        SignInManager<User> signInManager,
        ILogger<AuthController> logger,
        ITestUserStore testUserStore)
    {
        _context = context;
        _jwtService = jwtService;
        _signInManager = signInManager;
        _logger = logger;
        _testUserStore = testUserStore;
    }

    #endregion

    #region 🔓 Public Endpoints

    /// <summary>
    /// 🔑 Logs a user in by validating credentials and issuing a JWT.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDTO loginDto)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email!.ToLower() == loginDto.Email.ToLower());

        if (user == null)
        {
            _logger.LogWarning("❌ Login failed – user not found. Email: {Email}", loginDto.Email);
            return Unauthorized(new { message = "❌ User not found." });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("❌ Login attempt for inactive account. Email: {Email}", loginDto.Email);
            return Unauthorized(new { message = "❌ Account not yet activated. Password must be set." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("❌ Login failed – incorrect password. User: {UserName} ({Email})", user.UserName, user.Email);
            return Unauthorized(new { message = "❌ Incorrect password." });
        }

        _logger.LogInformation("✅ Successful login. User: {UserName} ({Email})", user.UserName, user.Email);

        var response = new AuthResponseDTO
        {
            Token = await _jwtService.CreateToken(user),
            RefreshToken = await _jwtService.CreateRefreshTokenAsync(user.Id),
            User = user.ToDTO()
        };

        return Ok(response);
    }

    /// <summary>
    /// 🚪 Logs out a user (just logs the request).
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var userEmail = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("🚪 User {Email} logged out.", userEmail);
        return Ok(new { message = "Logout logged." });
    }

    /// <summary>
    /// 🧾 Registers a new user (admin-triggered, not public).
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "Admin,GlobalAdmin")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("❌ Registration failed – invalid model state.");
            return BadRequest(ModelState);
        }

        var allowedRoles = new[] { "User", "Manager", "Admin" };

        if (!allowedRoles.Contains(registerDto.Role))
        {
            _logger.LogWarning("❌ Registration failed: Invalid role '{Role}'", registerDto.Role);
            return BadRequest(new { message = $"❌ Invalid role: '{registerDto.Role}'." });
        }

        if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email!.ToLower() == registerDto.Email.ToLower()))
        {
            _logger.LogWarning("❌ Registration failed – email already in use: {Email}", registerDto.Email);
            return BadRequest(new { message = "❌ Email is already registered." });
        }

        var user = new User
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            Name = registerDto.Name,
            NeedsPasswordSetup = true,
            IsActive = false,
            Role = registerDto.Role
        };

        var result = await _signInManager.UserManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("❌ Registration failed for {Email}. Errors: {Errors}", user.Email, result.Errors.Select(e => e.Description));
            return BadRequest(new
            {
                message = "❌ Registration failed.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        _logger.LogInformation("✅ Registration successful. Awaiting password setup for {Email}", user.Email);

        return StatusCode(201, new { message = "✅ User has been marked and must set his password." });
    }

    /// <summary>
    /// 🔑 Sets a new password and activates the user.
    /// </summary>
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordDTO dto)
    {
        _logger.LogInformation("🔑 SetPassword attempt for {Email}", dto.Email);

        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            _logger.LogWarning("❌ SetPassword failed – missing email or password.");
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email!.ToLower() == dto.Email.ToLower());

        if (user is null || !user.NeedsPasswordSetup)
        {
            _logger.LogWarning("❌ SetPassword failed. No setup required or user not found: {Email}", dto.Email);
            return BadRequest(new { message = "❌ User not found or setup not required." });
        }

        if (await _signInManager.UserManager.HasPasswordAsync(user))
        {
            _logger.LogWarning("❌ SetPassword failed – user already has a password. {Email}", dto.Email);
            return BadRequest(new { message = "❌ User already has a password." });
        }

        var result = await _signInManager.UserManager.AddPasswordAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            _logger.LogError("❌ SetPassword failed for {Email}. Errors: {Errors}", user.Email, result.Errors.Select(e => e.Description));
            return BadRequest(new
            {
                message = "❌ Password could not be set.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        user.Name = dto.Name.Trim();
        user.NeedsPasswordSetup = false;
        user.IsActive = true;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Password successfully set and user activated: {Email}", user.Email);

        var token = await _jwtService.CreateToken(user);
        var refreshToken = await _jwtService.CreateRefreshTokenAsync(user.Id);

        return Ok(new AuthResponseDTO
        {
            Token = token,
            RefreshToken = refreshToken,
            User = user.ToDTO()
        });
    }

    /// <summary>
    /// 🔄 Completes a registration by assigning email and password.
    /// </summary>
    [HttpPost("complete-registration")]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationDTO dto)
    {
        _logger.LogInformation("🔄 Attempt to complete registration for email: {Email}", dto.Email);

        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            _logger.LogWarning("❌ CompleteRegistration failed – missing email or password.");
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u =>
                u.NeedsPasswordSetup &&
                string.IsNullOrEmpty(u.Email) &&
                !u.IsActive);

        if (user == null)
        {
            _logger.LogWarning("❌ CompleteRegistration failed – no matching user.");
            return NotFound(new { message = "❌ No suitable user found." });
        }

        user.UserName = dto.Email.Trim();
        user.Email = dto.Email.Trim();
        user.Name = dto.Name.Trim();
        user.NeedsPasswordSetup = false;
        user.IsActive = true;

        var result = await _signInManager.UserManager.AddPasswordAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            _logger.LogError("❌ CompleteRegistration failed for {Email}. Errors: {Errors}", user.Email, result.Errors.Select(e => e.Description));
            return BadRequest(new
            {
                message = "❌ Password could not be set.",
                errors = result.Errors.Select(e => e.Description)
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Registration completed and user activated: {Email}", user.Email);

        var response = new AuthResponseDTO
        {
            Token = await _jwtService.CreateToken(user),
            RefreshToken = await _jwtService.CreateRefreshTokenAsync(user.Id),
            User = user.ToDTO()
        };

        return Ok(response);
    }

    /// <summary>
    /// 🔁 Refreshes a JWT token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO dto)
    {
        _logger.LogInformation("🔁 Received refresh request with token: {Token}", dto.RefreshToken);

        try
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

            if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("❌ Invalid or expired refresh token.");
                return Unauthorized(new { message = "❌ Invalid or expired refresh token." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == storedToken.UserId);
            if (user == null)
            {
                _logger.LogWarning("❌ User not found for refresh token.");
                return Unauthorized(new { message = "❌ User not found." });
            }

            var newAccessToken = await _jwtService.CreateToken(user);
            var newRefreshToken = await _jwtService.CreateRefreshTokenAsync(user.Id);

            _logger.LogInformation("✅ Token successfully renewed for user {UserId}", user.Id);

            return Ok(new AuthResponseDTO
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                User = user.ToDTO()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception during token refresh.");
            return BadRequest(new { message = "❌ Token processing failed.", error = ex.Message });
        }
    }

    /// <summary>
    /// 🧪 Returns test users (only available in dev mode).
    /// </summary>
    [HttpGet("testusers")]
    [AllowAnonymous]
    public ActionResult<List<UserDTO>> GetTestUsers()
    {
        var users = _testUserStore.TestUsers;
        return Ok(users);
    }

    #endregion
}



///// @file AuthController.cs
///// @author Claus Falkenstein
///// @company VIA Software GmbH
///// @date 2025-04-27
///// @brief Provides authentication endpoints for login and registration.
///// @details
///// This controller handles user authentication, including login, logout,
///// password setup, registration, and token refresh.
///// JWTs and RefreshTokens are issued upon successful authentication.
/////
///// @endpoints
///// - POST /api/auth/login → Login for users
///// - POST /api/auth/logout → Logs out a user (log only)
///// - POST /api/auth/register → Admin-only registration
///// - POST /api/auth/set-password → Sets password and activates account
///// - POST /api/auth/complete-registration → Completes account setup
///// - POST /api/auth/refresh → Refreshes JWT using a valid refresh token

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.IdentityModel.Tokens.Jwt;
//using UserFlow.API.Data;
//using UserFlow.API.Data.Entities;
//using UserFlow.API.Helpers;
//using UserFlow.API.Services;
//using UserFlow.API.Shared.DTO;
//using UserFlow.API.Shared.DTO.Auth;

//namespace UserFlow.API.Controllers;

///// <summary>
///// 🔐 Handles authentication logic for login, registration, token refresh, etc.
///// </summary>
//[ApiController]
//[Route("api/auth")]
//public class AuthController : ControllerBase
//{
//    #region 🔒 Fields

//    /// <summary>📦 Database context</summary>
//    private readonly AppDbContext _context;

//    /// <summary>🔐 JWT service for token generation</summary>
//    private readonly IJwtService _jwtService;

//    /// <summary>👤 Sign-in manager (Identity)</summary>
//    private readonly SignInManager<User> _signInManager;

//    /// <summary>📝 Logger instance</summary>
//    private readonly ILogger<AuthController> _logger;

//    /// <summary>👤 Test user store for demo purposes</summary>
//    private readonly ITestUserStore _testUserStore;

//    #endregion

//    #region 🏗️ Constructor

//    /// <summary>
//    /// 🧱 Constructor injecting required services.
//    /// </summary>
//    public AuthController(
//        AppDbContext context, 
//        IJwtService jwtService, 
//        SignInManager<User> signInManager, 
//        ILogger<AuthController> logger,
//        ITestUserStore testUserStore)
//    {
//        _context = context;
//        _jwtService = jwtService;
//        _signInManager = signInManager;
//        _logger = logger;
//        _testUserStore = testUserStore;
//    }

//    #endregion

//    #region 🔓 Public Endpoints

//    /// <summary>
//    /// 🔑 Logs a user in by validating credentials and issuing a JWT.
//    /// </summary>
//    [HttpPost("login")]
//    [AllowAnonymous]
//    public async Task<IActionResult> Login(LoginDTO loginDto)
//    {
//        /// 🔍 Find user by email (case-insensitive, ignoring filters like IsDeleted)
//        var user = await _context.Users
//            .IgnoreQueryFilters() // ⚠️ Ignoring soft-delete filter
//            .FirstOrDefaultAsync(u => u.Email!.ToLower() == loginDto.Email.ToLower());

//        /// ❌ User not found
//        if (user == null)
//        {
//            _logger.LogWarning("❌ Login failed – user not found. Email: {Email}", loginDto.Email);
//            return Unauthorized(new { message = "❌ User not found." });
//        }

//        /// ❌ User not yet activated
//        if (!user.IsActive)
//        {
//            return Unauthorized(new { message = "❌ Account not yet activated. Password must be set." });
//        }

//        /// 🔐 Check password
//        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
//        if (!result.Succeeded)
//        {
//            _logger.LogWarning("❌ Login failed – incorrect password. User: {UserName} ({Email})", user.UserName, user.Email);
//            return Unauthorized(new { message = "❌ Incorrect password." });
//        }

//        /// ✅ Successful login
//        _logger.LogInformation("\n\r✅ Successful login. User: {UserName} ({Email})", user.UserName, user.Email);

//        /// 🧾 Create authentication response
//        var response = new AuthResponseDTO
//        {
//            Token = await _jwtService.CreateToken(user), // 🔑 Issue JWT
//            RefreshToken = await _jwtService.CreateRefreshTokenAsync(user.Id), // 🔁 Create refresh token
//            User = user.ToDTO() // 📤 Map user to DTO
//        };

//        return Ok(response); // 📦 Return tokens + user info
//    }

//    /// <summary>
//    /// 🚪 Logs out a user (just logs the request).
//    /// </summary>
//    [HttpPost("logout")]
//    public IActionResult Logout()
//    {
//        var userEmail = User.Identity?.Name ?? "Unknown"; // 🔎 Get current user email
//        _logger.LogInformation("🚪 Logout requested by {Email}\n\r", userEmail);
//        return Ok(new { message = "Logout logged." });
//    }

//    /// <summary>
//    /// 🧾 Registers a new user (admin-triggered, not public).
//    /// </summary>
//    [HttpPost("register")]
//    [Authorize(Roles = "Admin,GlobalAdmin")]
//    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
//    {
//        if (!ModelState.IsValid)
//            return BadRequest(ModelState); // ❗Invalid input

//        var allowedRoles = new[] { "User", "Manager", "Admin" };

//        /// ❌ Role not allowed
//        if (!allowedRoles.Contains(registerDto.Role))
//        {
//            _logger.LogWarning("❌ Registration failed: Invalid role '{Role}'", registerDto.Role);
//            return BadRequest(new { message = $"❌ Invalid role: '{registerDto.Role}'." });
//        }

//        /// ❌ Email already exists
//        if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email!.ToLower() == registerDto.Email.ToLower()))
//        {
//            _logger.LogWarning("❌ Registration failed – email already in use: {Email}", registerDto.Email);
//            return BadRequest(new { message = "❌ Email is already registered." });
//        }

//        /// 👤 Create user with setup required
//        var user = new User
//        {
//            UserName = registerDto.Email,
//            Email = registerDto.Email,
//            Name = registerDto.Name,
//            NeedsPasswordSetup = true,
//            IsActive = false,
//            Role = registerDto.Role
//        };

//        var result = await _signInManager.UserManager.CreateAsync(user);
//        if (!result.Succeeded)
//        {
//            _logger.LogError("❌ Registration failed for {Email}. Errors: {Errors}", user.Email, result.Errors.Select(e => e.Description));
//            return BadRequest(new
//            {
//                message = "❌ Registration failed.",
//                errors = result.Errors.Select(e => e.Description)
//            });
//        }

//        _logger.LogInformation("✅ Registration successful. Awaiting password setup for {Email}", user.Email);

//        return StatusCode(201, new { message = "✅ User has been marked and must set his password." });
//    }

//    /// <summary>
//    /// 🔑 Sets a new password and activates the user.
//    /// </summary>
//    [HttpPost("set-password")]
//    public async Task<IActionResult> SetPassword([FromBody] SetPasswordDTO dto)
//    {
//        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
//            return BadRequest(new { message = "Email and password are required." });

//        /// 🔍 Find user by email
//        var user = await _context.Users
//            .IgnoreQueryFilters()
//            .FirstOrDefaultAsync(u => u.Email!.ToLower() == dto.Email.ToLower());

//        /// ❌ Not eligible for setup
//        if (user is null || !user.NeedsPasswordSetup)
//        {
//            _logger.LogWarning("❌ SetPassword failed. No setup required or user not found: {Email}", dto.Email);
//            return BadRequest(new { message = "❌ User not found or setup not required." });
//        }

//        if (await _signInManager.UserManager.HasPasswordAsync(user))
//        {
//            _logger.LogWarning("❌ SetPassword failed – user already has a password. {Email}", dto.Email);
//            return BadRequest(new { message = "❌ User already has a password." });
//        }

//        /// 🔑 Set password
//        var result = await _signInManager.UserManager.AddPasswordAsync(user, dto.Password);
//        if (!result.Succeeded)
//        {
//            _logger.LogError("❌ SetPassword failed for {Email}. Errors: {Errors}", user.Email, result.Errors.Select(e => e.Description));
//            return BadRequest(new
//            {
//                message = "❌ Password could not be set.",
//                errors = result.Errors.Select(e => e.Description)
//            });
//        }

//        /// 🟢 Activate user
//        user.Name = dto.Name.Trim();
//        user.NeedsPasswordSetup = false;
//        user.IsActive = true;

//        await _context.SaveChangesAsync(); // 💾 Persist changes

//        _logger.LogInformation("✅ Password successfully set for {Email}", user.Email);

//        var token = await _jwtService.CreateToken(user);
//        var refreshToken = await _jwtService.CreateRefreshTokenAsync(user.Id);

//        return Ok(new AuthResponseDTO
//        {
//            Token = token,
//            RefreshToken = refreshToken,
//            User = user.ToDTO()
//        });
//    }

//    /// <summary>
//    /// 🔄 Completes a registration by assigning email and password.
//    /// </summary>
//    [HttpPost("complete-registration")]
//    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationDTO dto)
//    {
//        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
//            return BadRequest(new { message = "Email and password are required." });

//        /// 🔍 Search for anonymous user that needs setup
//        var user = await _context.Users
//            .IgnoreQueryFilters()
//            .FirstOrDefaultAsync(u =>
//                u.NeedsPasswordSetup &&
//                string.IsNullOrEmpty(u.Email) &&
//                !u.IsActive);

//        if (user == null)
//        {
//            _logger.LogWarning("❌ CompleteRegistration failed – no matching user.");
//            return NotFound(new { message = "❌ No suitable user found." });
//        }

//        /// 📝 Assign email + activate user
//        user.UserName = dto.Email.Trim();
//        user.Email = dto.Email.Trim();
//        user.Name = dto.Name.Trim();
//        user.NeedsPasswordSetup = false;
//        user.IsActive = true;

//        var result = await _signInManager.UserManager.AddPasswordAsync(user, dto.Password);
//        if (!result.Succeeded)
//        {
//            _logger.LogError("❌ CompleteRegistration failed for {Email}. Errors: {Errors}", user.Email, result.Errors.Select(e => e.Description));
//            return BadRequest(new
//            {
//                message = "❌ Password could not be set.",
//                errors = result.Errors.Select(e => e.Description)
//            });
//        }

//        await _context.SaveChangesAsync();

//        _logger.LogInformation("✅ Registration completed for {Email}", user.Email);

//        var response = new AuthResponseDTO
//        {
//            Token = await _jwtService.CreateToken(user),
//            RefreshToken = await _jwtService.CreateRefreshTokenAsync(user.Id),
//            User = user.ToDTO()
//        };

//        return Ok(response);
//    }

//    /// <summary>
//    /// 🔁 Refreshes a JWT token using a valid refresh token.
//    /// </summary>
//    /// <param name="dto">Refresh token request containing only the refresh token.</param>
//    /// <returns>New access and refresh tokens if successful, otherwise 401 or 400.</returns>
//    [HttpPost("refresh")]
//    [AllowAnonymous]
//    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO dto)
//    {
//        _logger.LogInformation("🔁 Received refresh request with token: {Token}", dto.RefreshToken);

//        try
//        {
//            // 🔍 Find the refresh token in the database
//            var storedToken = await _context.RefreshTokens
//                .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

//            // ❌ Not found or expired
//            if (storedToken is null || storedToken.ExpiresAt < DateTime.UtcNow)
//            {
//                _logger.LogWarning("❌ Invalid or expired refresh token.");
//                return Unauthorized(new { message = "❌ Invalid or expired refresh token." });
//            }

//            // 👤 Load the user
//            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == storedToken.UserId);
//            if (user == null)
//            {
//                _logger.LogWarning("❌ User not found for refresh token.");
//                return Unauthorized(new { message = "❌ User not found." });
//            }

//            // 🔑 Create new tokens
//            var newAccessToken = await _jwtService.CreateToken(user);
//            var newRefreshToken = await _jwtService.CreateRefreshTokenAsync(user.Id);

//            _logger.LogInformation("✅ Token successfully renewed for user {UserId}", user.Id);

//            return Ok(new AuthResponseDTO
//            {
//                Token = newAccessToken,
//                RefreshToken = newRefreshToken,
//                User = user.ToDTO()
//            });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "❌ Exception during token refresh.");
//            return BadRequest(new { message = "❌ Token processing failed.", error = ex.Message });
//        }
//    }

//    [HttpGet("testusers")]
//    [AllowAnonymous]
//    public ActionResult<List<UserDTO>> GetTestUsers()
//    {
//        var users = _testUserStore.TestUsers;
//        return Ok(users);
//    }
//    #endregion
//}
