/// @file CompanyMapper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Contains mapping logic from Company entities to CompanyDTO projections.
/// @details
/// Provides a compiled Expression-based projection for use in EF Core queries, enabling conditional inclusion
/// of related User data for Company-based DTOs.

using System.Linq.Expressions;
using UserFlow.API.Data.Entities;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Mappers;

/// <summary>
/// 👉 ✨ Provides mapping logic for converting <see cref="Company"/> entities into <see cref="CompanyDTO"/>s.
/// </summary>
public static class CompanyMapper
{
    /// <summary>
    /// 👉 ✨ Returns an expression for projecting a <see cref="Company"/> into a <see cref="CompanyDTO"/>.
    /// </summary>
    /// <param name="includeUsers">Whether to include related users in the projection.</param>
    /// <returns>A compiled <see cref="Expression"/> usable in EF Core queries.</returns>
    public static Expression<Func<Company, CompanyDTO>> ToCompanyDto(bool includeUsers = false)
    {
        /// 👥 Include user list with detailed user data if requested
        if (includeUsers)
        {
            return company => new CompanyDTO
            {
                Id = company.Id,
                Name = company.Name,
                Address = company.Address,
                PhoneNumber = company.PhoneNumber,
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt,
                IsDeleted = company.IsDeleted,
                UserCount = company.Users.Count,
                Users = company.Users.Select(user => new UserDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email ?? string.Empty,
                    Role = user.Role,
                    NeedsPasswordSetup = user.NeedsPasswordSetup,
                    IsActive = user.IsActive,
                    CompanyId = user.CompanyId,
                    CompanyName = user.Company != null ? user.Company.Name : string.Empty,
                    Company = null // 🧼 Prevent recursive object mapping
                }).ToList()
            };
        }

        /// 🧾 Base mapping without nested Users list
        return company => new CompanyDTO
        {
            Id = company.Id,
            Name = company.Name,
            Address = company.Address,
            PhoneNumber = company.PhoneNumber,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt,
            IsDeleted = company.IsDeleted,
            UserCount = company.Users.Count,
            Users = null // 🚫 Skip users to reduce payload
        };
    }
}

/// @remarks
/// Developer Notes:
/// - ⚡ Uses compiled `Expression<Func<>>` to allow projection directly in EF Core LINQ queries.
/// - 🔁 Includes users only when explicitly requested via `includeUsers = true`.
/// - 🧼 Prevents circular references by nulling `Company` inside nested `UserDTO`.
/// - 🚀 Ensures optimal performance by avoiding unnecessary navigation property loading.
/// - 📦 Extendable: You can later add fields like Projects, Notes, or Statistics to this DTO mapping.
