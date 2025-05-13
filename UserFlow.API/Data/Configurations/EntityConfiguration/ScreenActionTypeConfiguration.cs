/// @file ScreenActionTypeConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief EF Core configuration for the ScreenActionType entity.
/// @details
/// Defines the schema for categorizing screen actions, including name, description, and
/// support for soft deletion.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Configures the schema for the <c>ScreenActionType</c> entity.
/// </summary>
public class ScreenActionTypeConfiguration : IEntityTypeConfiguration<ScreenActionType>
{
    /// <summary>
    /// 🛠 Configures schema rules and property constraints for ScreenActionType.
    /// </summary>
    /// <param name="builder">🔧 Fluent builder for ScreenActionType</param>
    public void Configure(EntityTypeBuilder<ScreenActionType> builder)
    {
        /// 🔑 Define primary key
        builder.HasKey(sat => sat.Id);

        /// 🏷 Required name (e.g., "Click", "Swipe")
        builder.Property(sat => sat.Name)
            .IsRequired()                 // ❗ Must be set
            .HasMaxLength(100);           // 🔠 Max length to enforce consistency

        /// 📝 Optional description of what this action type represents
        builder.Property(sat => sat.Description)
            .HasMaxLength(500);           // 🔠 Limit to 500 chars

        /// 🗑 Soft delete flag (default: false)
        builder.Property(sat => sat.IsDeleted)
            .HasDefaultValue(false);      // 🚫 Default to not deleted
    }
}

/// @remarks
/// Developer Notes:
/// - 🧠 ScreenActionTypes are used to classify user-triggered actions like "Click", "Swipe", etc.
/// - 🔍 Helps group and filter actions during analysis or rendering.
/// - 🗑 Soft delete is supported using the IsDeleted flag (default: false).
/// - ✅ Make sure each action type has a clear, unique purpose to avoid confusion.
/// - 🧩 Extend this entity carefully when adding behavior-specific attributes or localization.
