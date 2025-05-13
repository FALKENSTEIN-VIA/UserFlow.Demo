/// @file ScreenActionConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief EF Core configuration for the ScreenAction entity.
/// @details
/// Defines the schema for user-triggered screen actions, including relationships to
/// screens, projects, users, action types, and successor screens. Implements soft delete
/// and ensures DeleteBehavior consistency to avoid navigation issues.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Configures the schema and relationships for the <c>ScreenAction</c> entity.
/// </summary>
public class ScreenActionConfiguration : IEntityTypeConfiguration<ScreenAction>
{
    /// <summary>
    /// 🛠 Applies Fluent API rules for the ScreenAction table.
    /// </summary>
    /// <param name="builder">🔧 Builder to configure entity properties and relationships</param>
    public void Configure(EntityTypeBuilder<ScreenAction> builder)
    {
        /// 🔑 Define primary key
        builder.HasKey(sa => sa.Id);

        /// 🏷 Required name (max 200 characters)
        builder.Property(sa => sa.Name)
            .IsRequired()
            .HasMaxLength(200);

        /// 🗒 Optional event description (max 500 characters)
        builder.Property(sa => sa.EventDescription)
            .HasMaxLength(500);

        /// 🔢 Optional sort index with default value
        builder.Property(sa => sa.SortIndex)
            .HasDefaultValue(0);

        /// 🗑 Soft delete support
        builder.Property(sa => sa.IsDeleted)
            .HasDefaultValue(false);

        #region 🔗 Relationships

        /// 🖥 Required Screen (delete cascades to actions)
        builder.HasOne(sa => sa.Screen)
            .WithMany(s => s.ScreenActions)       // 🔄 Screen has many actions
            .HasForeignKey(sa => sa.ScreenId)     // 🔑 FK to Screen
            .OnDelete(DeleteBehavior.Cascade);    // 💣 Delete screen = delete actions

        /// 📁 Required Project (no back navigation)
        builder.HasOne(sa => sa.Project)
            .WithMany()                           // ⛔ No navigation in Project
            .HasForeignKey(sa => sa.ProjectId)    // 🔑 FK to Project
            .OnDelete(DeleteBehavior.Restrict);   // 🚫 Prevent cascade delete

        /// 👤 Required User (owner of the action)
        builder.HasOne(sa => sa.User)
            .WithMany()
            .HasForeignKey(sa => sa.UserId)
            .OnDelete(DeleteBehavior.Restrict);   // 🚫 Prevent user deletion from breaking relation

        /// 🎯 Required action type
        builder.HasOne(sa => sa.ScreenActionType)
            .WithMany(sat => sat.ScreenActions)   // 🔄 One type to many actions
            .HasForeignKey(sa => sa.ScreenActionTypeId)
            .OnDelete(DeleteBehavior.Restrict);   // 🚫 Prevent cascade delete

        /// 🔁 Optional successor screen
        builder.HasOne(sa => sa.SuccessorScreen)
            .WithMany()                           // ⛔ No back navigation
            .HasForeignKey(sa => sa.SuccessorScreenId)
            .OnDelete(DeleteBehavior.Restrict);   // 🚫 Prevent cascading delete

        #endregion
    }
}

/// @remarks
/// Developer Notes:
/// - 🧠 Represents a user-triggered event on a screen (click, swipe, etc.).
/// - 🔗 Linked to Screen, Project, User, ScreenActionType, and optionally SuccessorScreen.
/// - 🗑 Soft delete implemented via IsDeleted (default: false).
/// - 💣 DeleteBehavior:
///   - Screen → Cascade (deleting screen removes actions)
///   - Project/User/Type/Successor → Restrict (must exist for referential integrity)
/// - ⚠️ Avoid circular or shadow navigation by defining all FKs explicitly.
/// - 🧩 Can be extended to support logging, custom action payloads, or chaining logic.
