/// *****************************************************************************************
/// @file RoleCountDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief DTO representing the number of users assigned to a specific role.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 📊 DTO used to represent the number of users assigned to a specific role.
/// </summary>
public class RoleCountDTO
{
    /// <summary>
    /// 🎭 Name of the role (e.g., "Admin", "User", "Manager").
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// 🔢 Number of users assigned to this role.
    /// </summary>
    public int Count { get; set; }
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - This DTO is typically used in dashboards or statistical API responses.
/// - Can be extended with additional properties (e.g., CompanyId, RoleDescription).
/// - Used in endpoints that aggregate or group users by role.
/// *****************************************************************************************
