/// @file NoteConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-02
/// @brief EF Core configuration for the Note entity.
/// @details
/// Defines schema rules and relationships for notes, including constraints and 
/// consistent foreign key behavior to avoid EF Core shadow property conflicts.
/// Also ensures soft delete support and text validation.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Configures the schema and relationships for the <c>Note</c> entity.
/// </summary>
public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    /// <summary>
    /// 🛠 Defines all schema rules and relationships using Fluent API
    /// </summary>
    /// <param name="builder">🔧 EntityTypeBuilder for the Note entity</param>
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        /// 🔑 Define primary key
        builder.HasKey(n => n.Id); // 👉 ✨ Primary Key for Note entity

        #region 📝 Properties

        /// 🏷 Title (required, max 2000 characters)
        builder.Property(n => n.Title)
               .IsRequired()            // ❗ Title must be present
               .HasMaxLength(2000);     // 🔠 Limit length for performance and DB design

        /// 🧾 Content (required, free text)
        builder.Property(n => n.Content)
               .IsRequired();           // ❗ Content is mandatory

        /// 🗑 Soft delete flag
        builder.Property(n => n.IsDeleted)
               .HasDefaultValue(false); // 🚫 Default to not deleted

        #endregion

        #region 🔗 Relationships

        /// 👤 Relation to User (required, restrict delete)
        builder.HasOne(n => n.User)
               .WithMany()                         // 🔄 No navigation from User side
               .HasForeignKey(n => n.UserId)       // 🔑 Foreign key
               .OnDelete(DeleteBehavior.Restrict); // 🚫 Don't allow cascading delete

        /// 📁 Optional relation to Project
        builder.HasOne(n => n.Project)
               .WithMany(p => p.Notes)             // 🔄 Project has many Notes
               .HasForeignKey(n => n.ProjectId)    // 🔑 Foreign key
               .OnDelete(DeleteBehavior.SetNull);  // 🧹 Nullify on delete

        /// 🖥 Optional relation to Screen
        builder.HasOne(n => n.Screen)
               .WithMany(s => s.Notes)             // 🔄 Screen has many Notes
               .HasForeignKey(n => n.ScreenId)     // 🔑 Foreign key
               .OnDelete(DeleteBehavior.SetNull);  // 🧹 Nullify on delete

        #endregion
    }
}

/// @remarks
/// Developer Notes:
/// - 🧾 Notes must always include textual content.
/// - 🚫 Deleting a user does NOT delete notes (restrict behavior).
/// - 🧹 Projects or Screens can be deleted → Notes keep integrity via null.
/// - 🧠 Extending Notes (e.g., adding categories or timestamps) requires updating this config.
/// - ⚠️ Avoid introducing shadow properties by always defining explicit FKs.
