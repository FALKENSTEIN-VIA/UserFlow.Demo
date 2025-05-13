/// *****************************************************************************************
/// @file IUnitOfWork.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-09
/// @brief Interface for central access to all HTTP service endpoints.
/// @details
/// Combines all individual API service interfaces (Auth, Users, Projects, etc.)
/// into a single unit for streamlined dependency injection and modular usage.
/// *****************************************************************************************

//XXX
using UserFlow.API.Http.HubServices;
using UserFlow.API.Http.Services;

namespace UserFlow.API.HTTP;

/// <summary>
/// 👉 ✨ Centralized access point for all HTTP API services.
/// </summary>
public interface IUnitOfWork
{
    IHubService HubService { get; }

    /// <summary>🔐 Authentication and authorization service.</summary>
    IAuthService Auth { get; }

    /// <summary>👤 User management service (CRUD, restore, import/export).</summary>
    IUserService Users { get; }

    /// <summary>🏢 Company management service (admin + registration workflows).</summary>
    ICompanyService Companies { get; }

    /// <summary>🧑‍💼 Employee management service.</summary>
    IEmployeeService Employees { get; }

    /// <summary>📦 Project management service.</summary>
    IProjectService Projects { get; }

    /// <summary>📺 Screen management service (UI views).</summary>
    IScreenService Screens { get; }

    /// <summary>🎯 Screen action management (clicks, inputs, gestures).</summary>
    IScreenActionService ScreenActions { get; }

    /// <summary>🏷️ Action type definitions (e.g., tap, swipe).</summary>
    IScreenActionTypeService ScreenActionTypes { get; }

    /// <summary>📝 Note management service (comments, annotations).</summary>
    INoteService Notes { get; }

    /// <summary>📊 Dashboard service for metrics, import/export.</summary>
    IDashboardService Dashboard { get; }

}

/// <remarks>
/// 🛠️ **Developer Notes**
/// - 🔁 Groups all HTTP client-facing service interfaces under one injectable abstraction.
/// - 🚀 Allows feature groups (Pages/ViewModels) to depend on a single injected IUnitOfWork.
/// - 📦 Use `UnitOfWork` as a service aggregator for better testability and clean architecture.
/// - 🔐 All services use `AuthorizedHttpClient` to ensure secure, token-based communication.
/// - 📤 Ideal for MAUI/WPF clients interacting with the UserFlow WebAPI.
/// </remarks>
