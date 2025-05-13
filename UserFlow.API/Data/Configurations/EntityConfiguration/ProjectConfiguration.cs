/// @file ProjectConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief EF Core configuration for the Project entity.
/// @details
/// Defines the schema for projects, including user ownership and support for soft deletion
/// via the IsDeleted flag. Configures relationships and enforces delete behavior to avoid
/// duplicate foreign key conflicts.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Configures the <c>Project</c> entity schema and relationships.
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <summary>
    /// 🛠 Applies schema rules and relational configuration using Fluent API.
    /// </summary>
    /// <param name="builder">🔧 EntityTypeBuilder for Project</param>
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        /// 🔑 Set primary key (Id comes from BaseEntity)
        builder.HasKey(p => p.Id);

        /// 🏷 Required project name (max 200 characters)
        builder.Property(p => p.Name)
            .IsRequired()             // ❗ Project must have a name
            .HasMaxLength(200);       // 🔠 Limit for database efficiency

        /// 👤 Relation to User who owns this project
        builder.HasOne(p => p.User)
            .WithMany()                               // 🔄 No navigation property from User side
            .HasForeignKey(p => p.UserId)             // 🔑 Foreign key to User
            .OnDelete(DeleteBehavior.Restrict);       // 🚫 Prevent cascading delete (important for integrity)
        /// 🛠️ Prevents EF Core from introducing duplicate shadow keys

        /// 🗑 Soft delete support (default: not deleted)
        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);                  // 🚫 Deleted flag defaults to false

        /// 🔄 Shared flag: defines if project is visible to others
        builder.Property(p => p.IsShared)
            .HasDefaultValue(false);                  // 🔐 Only owner by default

        /// 🧠 Use global query filters in DbContext to hide soft-deleted and non-shared projects by default
    }
}

/// @remarks
/// Developer Notes:
/// - 👤 Each project belongs to one user via UserId.
/// - 🚫 Deletion of a User does not cascade to their Projects (DeleteBehavior.Restrict).
/// - 🗑 IsDeleted flag enables soft delete without physical removal.
/// - 🔐 IsShared allows controlled access to other users (handled in query logic).
/// - ⚠️ Always define foreign keys explicitly to avoid EF shadow property bugs.
/// - 🧠 Use HasQueryFilter in AppDbContext to automatically filter deleted projects.
