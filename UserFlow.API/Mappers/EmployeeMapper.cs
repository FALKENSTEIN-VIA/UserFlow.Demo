/// @file EmployeeMapper.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-07
/// @brief Contains projection logic for mapping Employee entities to EmployeeDTOs.
/// @details
/// Supports conditional inclusion of related Company data within the EmployeeDTO structure.
/// Designed for use in EF Core LINQ queries with compiled expressions.

using System.Linq.Expressions;
using UserFlow.API.Shared.DTO;

namespace UserFlow.API.Mappers;

/// <summary>
/// 👉 ✨ Provides mapping logic for converting <see cref="Employee"/> entities into <see cref="EmployeeDTO"/>s.
/// </summary>
public static class EmployeeMapper
{
    /// <summary>
    /// 👉 ✨ Returns an expression that maps an <see cref="Employee"/> to an <see cref="EmployeeDTO"/>.
    /// </summary>
    /// <param name="includeCompany">Whether to include related <see cref="CompanyDTO"/> data.</param>
    /// <returns>A compiled <see cref="Expression"/> usable in LINQ-to-Entities queries.</returns>
    public static Expression<Func<Employee, EmployeeDTO>> ToEmployeeDto(bool includeCompany = false)
    {
        /// 🏢 If company data should be included, populate the nested DTO
        if (includeCompany)
        {
            return employee => new EmployeeDTO
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Role = employee.Role,
                UserId = employee.UserId,
                CompanyId = employee.CompanyId,
                Company = employee.Company != null
                    ? new CompanyDTO
                    {
                        Id = employee.Company.Id,
                        Name = employee.Company.Name,
                        Address = employee.Company.Address,
                        PhoneNumber = employee.Company.PhoneNumber,
                        CreatedAt = employee.Company.CreatedAt,
                        UpdatedAt = employee.Company.UpdatedAt,
                        IsDeleted = employee.Company.IsDeleted
                    }
                    : null // 🚫 No company info if null
            };
        }

        /// 📦 Base mapping without company data
        return employee => new EmployeeDTO
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Role = employee.Role,
            UserId = employee.UserId,
            CompanyId = employee.CompanyId,
            Company = null // ❌ Exclude nested company
        };
    }
}

/// @remarks
/// Developer Notes:
/// - ⚡ Uses compiled `Expression<Func<>>` to ensure EF Core can translate mappings to SQL.
/// - 🏢 Company mapping is optional — pass `includeCompany = true` when needed (e.g. admin views).
/// - 🧼 Prevents circular references and unnecessary data loading by default.
/// - 📄 Extendable: Add additional nested mappings for future enhancements if required.
/// - 🚀 Always use projections like this for read-only scenarios to boost query performance.
