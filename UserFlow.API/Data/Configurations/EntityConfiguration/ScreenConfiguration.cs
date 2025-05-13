/// @file ScreenConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief EF Core configuration for the Screen entity.
/// @details
/// Defines the schema for individual screens within a project, including relationships
/// to projects, users, screen actions, and notes. Supports soft deletion via IsDeleted.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Configures the schema and relationships for the <c>Screen</c> entity.
/// </summary>
public class ScreenConfiguration : IEntityTypeConfiguration<Screen>
{
    /// <summary>
    /// 🛠 Configures properties and relationships for the <see cref="Screen"/> entity.
    /// </summary>
    /// <param name="builder">🔧 Fluent API builder for Screen</param>
    public void Configure(EntityTypeBuilder<Screen> builder)
    {
        /// 🔑 Primary key
        builder.HasKey(s => s.Id);

        /// 🆔 Unique identifier (e.g., for frontend reference)
        builder.Property(s => s.Identifier)
            .IsRequired()
            .HasMaxLength(200);

        /// 🧩 Optional screen type (e.g., "Form", "Detail", "List")
        builder.Property(s => s.Type)
            .HasMaxLength(100);

        /// 📝 Optional screen description
        builder.Property(s => s.Description)
            .HasMaxLength(500);

        /// 🗑 Soft delete support
        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);

        /// 🏷 Required name of the screen
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        #region 🔗 Relationships

        /// 📁 Each screen belongs to a project (cascading delete)
        builder.HasOne(s => s.Project)
            .WithMany(p => p.Screens)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade); // 💣 Delete project = delete screens

        /// 👤 Optional user reference (owner or last editor)
        builder.HasOne(s => s.User)
            .WithMany()                         // ⛔ No navigation from User
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict); // 🚫 No cascade delete

        /// 🎬 A screen can have many actions
        builder.HasMany(s => s.ScreenActions)
            .WithOne(sa => sa.Screen)
            .HasForeignKey(sa => sa.ScreenId)
            .OnDelete(DeleteBehavior.Cascade);  // 💣 Delete screen = delete actions

        #endregion
    }
}

/// @remarks
/// Developer Notes:
/// - 📱 Represents a single UI screen within a project.
/// - 📁 Each screen must belong to a project (required FK).
/// - 👤 A user may optionally be linked (e.g., creator or editor).
/// - 🧩 Related to ScreenActions (1:n).
/// - 🗑 Soft delete is supported via IsDeleted flag.
/// - 💣 Deleting a project or screen also deletes all associated actions (Cascade).
/// - ⚠️ Be careful when introducing bidirectional navigation – keep configuration clear to avoid EF Core conflicts.
