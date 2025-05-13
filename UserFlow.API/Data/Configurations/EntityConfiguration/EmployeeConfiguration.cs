/// @file EmployeeConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-03
/// @brief EF Core configuration for the Employee entity.
/// @details
/// Defines table schema, required fields, and relationships for the Employee entity,
/// including foreign keys to User and Company with DeleteBehavior.Restrict.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Configures the schema for the <c>Employee</c> entity.
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    /// <summary>
    /// 🛠 Configures entity properties and relationships using the Fluent API.
    /// </summary>
    /// <param name="builder">🔧 EF Core builder for the Employee entity</param>
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        /// 🔑 Define primary key
        builder.HasKey(e => e.Id); // 👉 Use inherited Id from BaseEntity

        #region 📋 Properties

        /// 🧑‍💼 Employee name (required, max 100 chars)
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        /// 📧 Email address (required, max 255 chars)
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        /// 🏷 Role name (required, max 50 chars)
        builder.Property(e => e.Role)
            .IsRequired()
            .HasMaxLength(50);

        #endregion

        #region 🔗 Relationships

        /// 👤 Relation to User entity (optional, restrict delete)
        builder.HasOne(e => e.User)
            .WithMany()                              // 🔄 No navigation back from User
            .HasForeignKey(e => e.UserId)            // 🔑 Foreign key
            .OnDelete(DeleteBehavior.Restrict)       // 🚫 Prevent cascade delete
            .IsRequired(false);                      // ❓ Optional reference

        /// 🏢 Relation to Company (optional, restrict delete)
        builder.HasOne(e => e.Company)
            .WithMany(c => c.Employees)              // 🔄 Company has many Employees
            .HasForeignKey(e => e.CompanyId)         // 🔑 FK: CompanyId
            .OnDelete(DeleteBehavior.Restrict)       // 🚫 Restrict delete
            .IsRequired(false);                      // ❗ Optional due to Company query filters

        #endregion
    }
}
