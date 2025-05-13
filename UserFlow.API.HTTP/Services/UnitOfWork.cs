/// *****************************************************************************************
/// @file UnitOfWork.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-05-09
/// @brief Provides a centralized access point for all HTTP-based services used in the UserFlow client.
/// @details
/// This class encapsulates all available API services in a single unit for simplified DI and usage.
/// All services are injected via constructor and exposed as read-only properties.
/// *****************************************************************************************

using UserFlow.API.Http.HubServices;
using UserFlow.API.Http.Services;

namespace UserFlow.API.HTTP;

/// <summary>
/// 👉 ✨ Concrete implementation of <see cref="IUnitOfWork"/> using DI-resolved services.
/// </summary>
public class UnitOfWork(
    IHubService hubService,
    IAuthService auth,
    IUserService users,
    IProjectService projects,
    IScreenService screens,
    INoteService notes,
    IEmployeeService employees,
    ICompanyService companies,
    IScreenActionService screenActions,
    IScreenActionTypeService screenActionTypes,
    IDashboardService dashboard) : IUnitOfWork
{
    /// <summary>
    /// 📊 Provides SignalR hub access for real-time updates and notifications.
    /// </summary>
    //XXX
    public IHubService HubService { get; } = hubService;

    /// <summary>
    /// 🔐 Provides access to authentication and token-related operations.
    /// </summary>
    public IAuthService Auth { get; } = auth;

    /// <summary>
    /// 👤 Provides user management operations such as CRUD, restore, bulk, and CSV import/export.
    /// </summary>
    public IUserService Users { get; } = users;

    /// <summary>
    /// 🏢 Provides company-related operations including registration and admin workflows.
    /// </summary>
    public ICompanyService Companies { get; } = companies;

    /// <summary>
    /// 👔 Provides employee CRUD, paging, bulk and CSV support.
    /// </summary>
    public IEmployeeService Employees { get; } = employees;

    /// <summary>
    /// 📦 Provides full project management functionality.
    /// </summary>
    public IProjectService Projects { get; } = projects;

    /// <summary>
    /// 📺 Provides CRUD and import/export operations for screens.
    /// </summary>
    public IScreenService Screens { get; } = screens;

    /// <summary>
    /// 🎬 Handles screen action CRUD, restore, filtered access, import/export.
    /// </summary>
    public IScreenActionService ScreenActions { get; } = screenActions;

    /// <summary>
    /// 🏷️ Provides support for action type CRUD, restore, pagination, and CSV operations.
    /// </summary>
    public IScreenActionTypeService ScreenActionTypes { get; } = screenActionTypes;

    /// <summary>
    /// 📝 Provides note handling: CRUD, restore, CSV import/export.
    /// </summary>
    public INoteService Notes { get; } = notes;

    /// <summary>
    /// 📊 Provides dashboard data like user/project counts, import/export.
    /// </summary>
    public IDashboardService Dashboard { get; } = dashboard;

}

/// <remarks>
/// 🛠️ Developer Notes:
/// - All services are injected via constructor (DI-ready).
/// - Use `IUnitOfWork` in ViewModels or services for simplified API access.
/// - Grouped and categorized properties for logical separation and readability.
/// - Ensures that each API client is easily testable and replaceable.
/// </remarks>
