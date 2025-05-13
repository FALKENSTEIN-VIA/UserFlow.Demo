/// @file AppUserConfiguration.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief EF Core configuration for the AppUser entity extending IdentityUser.
/// @details
/// Provides schema and constraints for the AppUser entity, focusing on email and username
/// requirements. This configuration can be extended with additional properties as needed.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserFlow.API.Data.Entities;

namespace UserFlow.API.Data.Configurations;

/// <summary>
/// 👉 ✨ Configures the schema for the <c>User</c> entity.
/// </summary>
public class AppUserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// 🛠 Configures properties and constraints for the AppUser entity.
    /// </summary>
    /// <param name="builder">🔧 Fluent API builder for the User entity</param>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        /// 🔑 Set primary key (IdentityUser<long> base)
        builder.HasKey(u => u.Id);

        /// 📧 Email address (required, unique, max 255 characters)
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        /// 🧑 Benutzername (required, max 100 characters)
        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(100);

        /// 🏷 Klarname des Benutzers (required, max 200 characters)
        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);
    }
}

/// @remarks
/// Developer Notes:
/// - 📧 Email is required and limited to 255 characters (matching Identity standards).
/// - 🧑 UserName is required (also used for login identity).
/// - 🏷 Name is the full display name of the user.
/// - ⚠️ Extend carefully to avoid conflicts with Identity fields (e.g., PasswordHash, NormalizedEmail).
/// - 🧠 Ideal place to add additional user-level properties (e.g., profile picture, preferences).
/// - 🛠️ Ensure uniqueness constraints are handled via Identity configuration elsewhere if needed.
