using UserFlow.API.Data.Entities;

/// <summary>
/// 👉 ✨ Represents a non-authenticated employee within a company.
/// Used for documenting staff members who may or may not have login accounts.
/// </summary>
public class Employee : BaseEntity
{
    /// <summary>
    /// 👤 Full name of the employee.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 📧 Contact email of the employee.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 🧩 Logical role or position of the employee (e.g., "Technician", "Manager").
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// 🏢 Foreign key reference to the company.
    /// </summary>
    public long? CompanyId { get; set; }

    /// <summary>
    /// 🏢 Navigation property to the associated company.
    /// </summary>
    public Company? Company { get; set; } = null!;

    /// <summary>
    /// 🔗 Optional link to a system user account (Identity user).
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 🔐 Navigation property to the linked system user (if exists).
    /// </summary>
    public User? User { get; set; }
}

/// @remarks
/// Developer Notes:
/// - 🧾 `Employee` entries represent organizational members without requiring login credentials.
/// - 🔗 Can optionally be linked to a `User` entity for authenticated access.
/// - 🏢 Each employee can be assigned to a `Company` (nullable for flexibility).
/// - 🧠 Ideal for displaying team structures, assigning responsibilities or tracking roles.
/// - ⚠️ Keep in sync with `User` entity if dual usage is expected (e.g., via seeding or admin linkage).
