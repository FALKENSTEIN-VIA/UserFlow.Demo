/// @file IUserOwned.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief Interface to indicate that an entity is owned by a specific user (multi-tenancy support).
/// @details
/// Entities implementing this interface are associated with a specific user via UserId.
/// Enables automatic data isolation, security enforcement, and user-specific filtering in multi-tenant environments.

namespace UserFlow.API.Data.Interfaces;

/// <summary>
/// 👉 ✨ Interface for entities that are owned by a specific user (multi-tenancy).
/// </summary>
public interface IUserOwned
{
    /// <summary>
    /// 👤 The ID of the user who owns the entity.
    /// </summary>
    long UserId { get; set; }
}

/// @remarks
/// Developer Notes:
/// - 🔐 Use for enforcing per-user access restrictions in multi-tenant systems.
/// - 🧠 Combine with ISoftDelete and audit fields for full lifecycle tracking.
/// - ⚙️ Automatically enforced via EF Core global query filters using CurrentUserService.UserId.
/// - 🚨 Ensure UserId is set on creation (typically in service layer or controller).
/// - 🚀 Consider role-based exceptions (e.g., Admin/GlobalAdmin) to bypass filters where appropriate.
/// - Recommended for entities like Project, Screen, ScreenAction, Note, and others tied to a user.
