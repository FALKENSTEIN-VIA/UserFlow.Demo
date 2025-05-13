/// @file UserMapper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Provides expression-based mapping for User entities to UserDTOs.
/// @details
/// Includes optional projection of nested <see cref="CompanyDTO"/> if specified.
/// Intended for optimized usage in EF Core LINQ queries.

using System.Linq.Expressions;
using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.DTO;

/// <summary>
/// 👉 ✨ Provides mapping logic for converting <see cref="User"/> entities into <see cref="UserDTO"/>s.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// 👉 ✨ Returns a compiled expression to map a <see cref="User"/> to a <see cref="UserDTO"/>.
    /// </summary>
    /// <param name="includeCompany">Whether to include nested <see cref="CompanyDTO"/> data.</param>
    /// <returns>An EF Core-compatible <see cref="Expression"/>.</returns>
    public static Expression<Func<User, UserDTO>> ToUserDto(bool includeCompany = false)
    {
        /// 🏢 Include company details if requested
        if (includeCompany)
        {
            return user => new UserDTO
            {
                Id = user.Id,                                     // 🔑 User ID
                Name = user.Name,                                 // 🧑 Display name
                Email = user.Email ?? string.Empty,               // 📧 Email (default to empty if null)
                Role = user.Role,                                 // 🛡️ Assigned role
                NeedsPasswordSetup = user.NeedsPasswordSetup,     // 🔐 Flag for first login
                IsActive = user.IsActive,                         // ✅ Active status
                CompanyId = user.CompanyId,                       // 🏢 Related company ID
                CompanyName = user.Company != null
                    ? user.Company.Name                           // 🏷️ Friendly name of company
                    : string.Empty,
                Company = user.Company != null
                    ? new CompanyDTO                              // 🧩 Nested company data
                    {
                        Id = user.Company.Id,
                        Name = user.Company.Name,
                        Address = user.Company.Address,
                        PhoneNumber = user.Company.PhoneNumber,
                        CreatedAt = user.Company.CreatedAt,
                        UpdatedAt = user.Company.UpdatedAt,
                        IsDeleted = user.Company.IsDeleted
                    }
                    : null
            };
        }

        /// 🧼 Mapping without full company object
        return user => new UserDTO
        {
            Id = user.Id,                                     // 🔑 User ID
            Name = user.Name,                                 // 🧑 Display name
            Email = user.Email ?? string.Empty,               // 📧 Email address
            Role = user.Role,                                 // 🛡️ Role
            NeedsPasswordSetup = user.NeedsPasswordSetup,     // 🔐 First-time login flag
            IsActive = user.IsActive,                         // ✅ Active/inactive status
            CompanyId = user.CompanyId,                       // 🏢 Company ID
            CompanyName = user.Company != null
                ? user.Company.Name                           // 🏷️ Short name of company
                : string.Empty,
            Company = null                                    // 🚫 No full company object included
        };
    }
}

/// @remarks
/// Developer Notes:
/// - ⚡ Uses `Expression<Func<>>` for optimal EF Core SQL translation.
/// - 🏢 Nested CompanyDTO is conditionally included for use in detailed user views or exports.
/// - 🧼 Always null-safe: avoids runtime exceptions on missing company links.
/// - ✨ Suitable for paginated user lists, filters, admin views, and audits.
/// - 🔐 Extendable for future fields such as LastLogin, Permissions, etc.
