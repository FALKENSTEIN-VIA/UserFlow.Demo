/// @file CompanyConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-03
/// @brief EF Core configuration for the Company entity.
/// @details
/// Defines schema, keys and relationships for the Company table including audit and soft delete support.
/// Configures required fields, max lengths, audit fields, and soft delete behavior using Fluent API.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Configures the schema for the <see cref="Company"/> entity.
/// </summary>
public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    /// <summary>
    /// 🛠 Method to configure EF Core schema rules for Company entity
    /// </summary>
    /// <param name="builder">🔧 Builder object for configuring entity properties</param>
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(c => c.Id); // 🔑 Set primary key to Id (from BaseEntity)

        builder.Property(c => c.Name)           // 🏷 Company name
            .IsRequired()                       // ❗ Required field
            .HasMaxLength(200);                 // 🔠 Max length 200 characters

        builder.Property(c => c.Address)        // 🏢 Optional address field
            .HasMaxLength(500);                 // 🔠 Max length 500 characters

        builder.Property(c => c.PhoneNumber)    // ☎️ Optional phone field
            .HasMaxLength(50);                  // 🔠 Max length 50 characters

        builder.Property(c => c.CreatedAt)      // 🕒 Creation timestamp
            .IsRequired();                      // ❗ Required field

        builder.Property(c => c.IsDeleted)      // 🗑 Soft delete flag
            .IsRequired()                       // ❗ Always set
            .HasDefaultValue(false);            // 🚫 Default to not deleted

        builder.Property(c => c.CreatedBy);     // 🧾 Audit: who created
        builder.Property(c => c.UpdatedBy);     // ✏️ Audit: who last updated
        builder.Property(c => c.UpdatedAt);     // 🕒 Audit: last updated time
    }
}
