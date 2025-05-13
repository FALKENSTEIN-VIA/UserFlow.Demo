/// @file DataSeeder.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-28
/// @brief Seeds the database with initial data for UserFlow application
/// @details
/// <para>Handles database seeding for:</para>
/// <list type="bullet">
/// <item><description>Identity roles and users</description></item>
/// <item><description>Company structure with employees</description></item>
/// <item><description>Projects with associated screens and actions</description></item>
/// <item><description>User notes and activity tracking</description></item>
/// </list>
/// <para>Supports multi-tenant architecture and development/test data generation.</para>

using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserFlow.API.Data.Entities;
using UserFlow.API.Services;
using UserFlow.API.Shared.DTO;
using ILogger = Serilog.ILogger;


namespace UserFlow.API.Data;

/// <summary>
/// Static class responsible for database seeding operations 🧪
/// </summary>
public static class DataSeeder
{
    #region 🔒 Private Fields  

    ///// <summary>
    ///// Serilog logger instance for seeding operations 📋
    ///// </summary>
    private static readonly ILogger _logger = Log.ForContext(typeof(DataSeeder));

    /// <summary>
    /// Shared random number generator with GUID-based seed 🎲
    /// </summary>
    private static readonly Random _rand = new(Guid.NewGuid().GetHashCode());

    /// <summary>
    /// 🔗 Reference to the database context, used during seeding 🔗
    /// </summary>
    private static AppDbContext _context = null!;


    /// <summary>
    /// 🔗 Reference to the TestUserStore, filled with test users after seeding.
    /// This list makes testing easier, as the client can use it to login with test users.
    /// </summary>
    private static ITestUserStore _testUserStore = null!;

    /// <summary>
    /// 🛡️ Role manager for managing identity roles and claims 
    /// </summary>
    private static RoleManager<IdentityRole<long>> _roleManager = null!;

    /// <summary>
    /// 🧑‍💼 User manager for managing user accounts and claims 
    /// </summary>
    private static UserManager<User> _userManager = null!;

    /// <summary>
    /// Temporary list of users for test output 🧪
    /// </summary>
    private static readonly List<UserDTO> _testUsers = [];

    #endregion

    #region 🧩 Predefined Action Types

    /// <summary>
    /// Creates predefined screen action types (e.g., Click, Submit) 🧱
    /// </summary>
    /// <returns>List of initial screen action types</returns>
    private static List<ScreenActionType> GetPredefinedActionTypes() => new()
    {
        new() { Name = "Click", Description = "Click action" },
        new() { Name = "Submit", Description = "Submit form" },
        new() { Name = "Navigate", Description = "Navigate to another screen" },
        new() { Name = "Open", Description = "Open something" },
        new() { Name = "Close", Description = "Close something" }
    };

    #endregion

    #region 🚀 Core Seeding Entry

    /// <summary>
    /// Main seeding method that orchestrates database population 🧪
    /// </summary>
    /// <param name="context">Database context</param>
    /// <returns>Async task</returns>
    public static async Task SeedAsync(
        AppDbContext context,
        RoleManager<IdentityRole<long>> roleManager,
        UserManager<User> userManager,
        ITestUserStore testUserStore)
    {
        /// Store reference to context
        _context = context;

        /// Store reference to role manager
        _roleManager = roleManager;

        /// Store reference to user manager
        _userManager = userManager;

        /// Store reference to test user store
        _testUserStore = testUserStore;

        /// Start database transaction 🔄
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            /// 👉 Seed identity roles 
            await DataSeeder.SeedRolesAsync(roleManager);

            /// 👉 Seed users and companies
            await DataSeeder.SeedUsersAndCompaniesAsync(_userManager, _context);

            /// 👉 Seed action types (only if not already present)
            if (!await context.ScreenActionTypes.IgnoreQueryFilters().AnyAsync())
            {
                var types = GetPredefinedActionTypes();
                await context.ScreenActionTypes.AddRangeAsync(types);
                await context.SaveChangesAsync();
            }

            /// 👉 Seed projects (only if not already present)
            if (!await context.Projects.IgnoreQueryFilters().AnyAsync())
            {
                await SeedProjectsWithScreensAndActions(context);
            }

            /// 👉 Seed notes (only if not already present)
            if (!await context.Notes.IgnoreQueryFilters().AnyAsync())
            {
                await SeedNotes(context);
            }

            /// ✅ Commit transaction
            await transaction.CommitAsync();
        }
        catch
        {
            /// ❌ Rollback on failure
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region 🔐 Identity Seeding

    /// <summary>
    /// Seeds initial identity roles (GlobalAdmin, Admin, etc.) 🛡️
    /// </summary>
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<long>> roleManager)
    {
        var roles = new[] { "GlobalAdmin", "Admin", "Manager", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<long> { Name = role });
            }
        }
    }

    /// <summary>
    /// Seeds users and companies with related employee records 👥
    /// </summary>
    public static async Task SeedUsersAndCompaniesAsync(UserManager<User> userManager, AppDbContext context)
    {
        _logger.Information("🚀 Starting user and company seeding...");

        try
        {
            var faker = new Faker("de") { Random = new Randomizer(Guid.NewGuid().GetHashCode()) };

            /// ⛔ Skip if users already exist
            if (await userManager.Users.AnyAsync())
            {
                _logger.Information("✅ Users already exist. Skipping seeding.\n\r");
                return;
            }

            var allEmployees = new List<Employee>();

            /// 🌍 Create global admin user
            var globalAdmin = new User
            {
                UserName = "admin@userflow.com",
                Email = "admin@userflow.com",
                Name = "GlobalAdmin",
                CompanyId = null,
                EmailConfirmed = true,
                Role = "GlobalAdmin",
                IsActive = true,
                NeedsPasswordSetup = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var adminResult = await userManager.CreateAsync(globalAdmin, "Test123!");
            if (!adminResult.Succeeded)
            {
                var errors = string.Join(", ", adminResult.Errors.Select(e => e.Description));
                _logger.Error("Global admin creation failed: {Errors}", errors);
                throw new Exception($"Global admin creation failed: {errors}");
            }

            await userManager.AddToRoleAsync(globalAdmin, "GlobalAdmin");
            _logger.Debug("Created global admin {Email}", globalAdmin.Email);

            /// 🏢 Create fake companies
            _logger.Information($"🚀 Generating {DataSeederOptions.CompaniesCount} test companies...");
            var companies = new List<Company>();
            for (int i = 1; i <= DataSeederOptions.CompaniesCount; i++)
            {
                companies.Add(new Company
                {
                    Name = faker.Company.CompanyName(),
                    Address = faker.Address.FullAddress(),
                    PhoneNumber = faker.Phone.PhoneNumberFormat(1),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.Companies.AddRangeAsync(companies);
            await context.SaveChangesAsync();

            var dbCompanies = await context.Companies.IgnoreQueryFilters().AsNoTracking().OrderBy(c => c.Id).ToListAsync();

            /// 🔁 Create users for each company
            foreach (var company in dbCompanies)
            {
                _logger.Debug("Creating users for {Company}", company.Name);

                /// 🛠️ Admin
                var adminUser = new User
                {
                    UserName = $"admin.{company.Id}@company.com",
                    Email = $"admin.{company.Id}@company.com",
                    Name = faker.Name.FullName(),
                    CompanyId = company.Id,
                    EmailConfirmed = true,
                    Role = "Admin",
                    IsActive = true,
                    NeedsPasswordSetup = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Test123!");
                if (!result.Succeeded)
                {
                    _logger.Warning("Failed to create admin for {Company}: {Errors}",
                        company.Name,
                        string.Join(", ", result.Errors));
                    continue;
                }

                await userManager.AddToRoleAsync(adminUser, "Admin");
                allEmployees.Add(CreateEmployee(adminUser, "Admin", company.Id));

                /// 👔 Managers
                for (int i = 0; i < DataSeederOptions.ManagersCount; i++)
                {
                    var manager = await CreateUserWithRole(userManager, company.Id, "Manager", faker);
                    if (manager != null)
                    {
                        allEmployees.Add(CreateEmployee(manager, "Manager", company.Id));
                    }
                }

                /// 👷 Regular Users
                for (int i = 0; i < DataSeederOptions.UserCount; i++)
                {
                    var user = await CreateUserWithRole(userManager, company.Id, "User", faker);
                    if (user != null)
                    {
                        allEmployees.Add(CreateEmployee(user, "User", company.Id));
                    }
                }
            }

            /// 💾 Save all employees
            if (allEmployees.Count > 0)
            {
                _logger.Information("🚀 Inserting {Count} employees...", allEmployees.Count);
                await context.Employees.AddRangeAsync(allEmployees);
                await context.SaveChangesAsync();
            }
            _logger.Information("🎉 Successfully seeded users and companies!");
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "🔥 Critical error during seeding!");
            throw;
        }
    }

    #endregion

    #region 🧰 Helper Methods

    /// <summary>
    /// Creates a user with a specific role using Faker-generated data 🧑‍💼
    /// </summary>
    private static async Task<User?> CreateUserWithRole(
        UserManager<User> userManager,
        long companyId,
        string role,
        Faker faker)
    {
        /// 🧾 Generate fake names and email
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var emailName = $"{firstName}.{lastName}".ToLowerInvariant();
        var email = $"{emailName}@company{companyId}.com";

        var user = new User
        {
            UserName = $"{firstName}_{lastName}",
            Name = $"{firstName} {lastName}",
            Email = email,
            CompanyId = companyId,
            EmailConfirmed = true,
            Role = role,
            IsActive = true,
            NeedsPasswordSetup = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "Test123!");
        return result.Succeeded ? user : null;
    }

    /// <summary>
    /// Creates an employee record based on the user 👤
    /// </summary>
    private static Employee CreateEmployee(User user, string role, long companyId) => new()
    {
        Name = user.Name,
        Email = user.Email!,
        Role = role,
        CompanyId = companyId,
        UserId = user.Id
    };

    #endregion

    #region 🏗️ Project Data Seeding

    /// <summary>
    /// Seeds projects, screens, and screen actions for each user 🏢
    /// </summary>
    private static async Task SeedProjectsWithScreensAndActions(AppDbContext context)
    {
        _logger.Information("🚀 Starting project, screen, and action seeding...");

        /// 📦 Load all users that belong to a company
        var users = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.CompanyId != null)
            .ToListAsync();

        if (!users.Any())
        {
            _logger.Warning("⚠️ No users with company assignment found - aborting seeding.");
            return;
        }

        /// 📋 Load screen action types
        var actionTypes = await context.ScreenActionTypes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        var faker = new Faker();
        var allProjects = new List<Project>();
        var allScreens = new List<Screen>();
        var allActions = new List<ScreenAction>();
        var projectToCompany = new Dictionary<long, long>();

        /// 🧪 Generate projects for each user
        foreach (var user in users)
        {
            var projects = new Faker<Project>()
                .RuleFor(p => p.Name, f => $"{f.Company.CatchPhrase()}")
                .RuleFor(p => p.Description, f => f.Lorem.Sentence())
                .RuleFor(p => p.UserId, user.Id)
                .RuleFor(p => p.CompanyId, _ => user.CompanyId!.Value)
                .RuleFor(p => p.IsShared, f => f.Random.Bool(0.05f))
                .Generate(DataSeederOptions.ProjectsPerUser);

            allProjects.AddRange(projects);
        }

        await context.Projects.AddRangeAsync(allProjects);
        await context.SaveChangesAsync();
        _logger.Information("✅ Successfully saved {Count} projects.", allProjects.Count);

        /// 🧪 Generate screens for each project
        foreach (var project in allProjects)
        {
            projectToCompany[project.Id] = project.CompanyId;

            var screens = new Faker<Screen>()
                .RuleFor(s => s.Name, f => $"{f.Hacker.Noun()} Screen")
                .RuleFor(s => s.Identifier, f => f.Random.AlphaNumeric(8))
                .RuleFor(s => s.ProjectId, project.Id)
                .RuleFor(s => s.UserId, project.UserId)
                .RuleFor(s => s.CompanyId, project.CompanyId)
                .Generate(DataSeederOptions.ScreensPerProject);

            allScreens.AddRange(screens);
        }

        await context.Screens.AddRangeAsync(allScreens);
        await context.SaveChangesAsync();
        _logger.Information("✅ Successfully saved {Count} screens.", allScreens.Count);

        /// 🧪 Generate screen actions for each screen
        foreach (var screen in allScreens)
        {
            var companyId = projectToCompany.GetValueOrDefault(screen.ProjectId);

            var actions = new Faker<ScreenAction>()
                .RuleFor(a => a.Name, f => $"{f.Hacker.Verb()} Action")
                .RuleFor(a => a.SortIndex, f => f.Random.Int(0, 100))
                .RuleFor(a => a.ScreenActionTypeId, f => f.PickRandom(actionTypes).Id)
                .RuleFor(a => a.ScreenId, screen.Id)
                .RuleFor(a => a.ProjectId, screen.ProjectId)
                .RuleFor(a => a.UserId, screen.UserId)
                .RuleFor(a => a.CompanyId, _ => companyId)
                .Generate(3);

            allActions.AddRange(actions);
        }

        await context.ScreenActions.AddRangeAsync(allActions);
        await context.SaveChangesAsync();
        _logger.Information("✅ Successfully saved {Count} screen actions.", allActions.Count);
        _logger.Information("✅ DATA SEEDING was successfully completed!" + Environment.NewLine);
    }

    #endregion

    #region 📝 Note Seeding

    /// <summary>
    /// Seeds notes for each screen 🗒️
    /// </summary>
    private static async Task SeedNotes(AppDbContext context)
    {
        var screens = await context.Screens
            .IgnoreQueryFilters()
            .Include(s => s.Project)
            .AsNoTracking()
            .ToListAsync();

        var notes = screens.Select(s => new Note
        {
            Title = new Faker().Lorem.Sentence(),
            Content = new Faker().Lorem.Paragraph(),
            ScreenId = s.Id,
            ProjectId = s.ProjectId,
            UserId = s.UserId,
            CompanyId = s.Project!.CompanyId
        }).ToList();

        await context.Notes.AddRangeAsync(notes);
        await context.SaveChangesAsync();
    }

    #endregion

    #region 👤 User Initialization

    /// <summary>
    /// Seeds initial projects and screens for a single user 🧪
    /// </summary>
    public static async Task SeedInitialDataForNewUserAsync(AppDbContext context, long userId)
    {
        _logger.Information("🚀 Starting initial data seeding for user {UserId}...", userId);

        var user = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.Warning("⚠️ User with ID {UserId} not found. Skipping seeding.", userId);
            return;
        }

        if (user.CompanyId == null)
        {
            _logger.Information("⏩ Skipping seeding for user {UserId} (no company assigned)", userId);
            return;
        }

        var faker = new Faker();
        var projects = new List<Project>();
        var screens = new List<Screen>();

        for (int i = 0; i < DataSeederOptions.ProjectsPerUser; i++)
        {
            projects.Add(new Project
            {
                Name = $"Project {i + 1}",
                Description = $"Auto-generated for {user.Email}",
                UserId = user.Id,
                CompanyId = user.CompanyId.Value,
                IsShared = _rand.Next(10) == 0
            });
        }

        await context.Projects.AddRangeAsync(projects);
        await context.SaveChangesAsync();

        foreach (var project in projects)
        {
            for (int j = 0; j < DataSeederOptions.ScreensPerProject; j++)
            {
                screens.Add(new Screen
                {
                    Name = $"Screen {j + 1}",
                    Identifier = Guid.NewGuid().ToString("N")[..8],
                    ProjectId = project.Id,
                    UserId = user.Id,
                    CompanyId = user.CompanyId.Value
                });
            }
        }

        await context.Screens.AddRangeAsync(screens);
        await context.SaveChangesAsync();

        _logger.Information("✅ Completed initial data seeding for user {UserId}", userId);
    }

    #endregion

    #region 📁 Save Login Lists

    /// <summary>
    /// 📋 Creates test user store for client-side development (easier login) 
    /// </summary>
    public static async Task SaveTestUserStore(AppDbContext context, ITestUserStore testUserStore)
    {
        var testUsers = await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(u => u.Company)
            .Select(u => new UserDTO
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email ?? string.Empty,
                Role = u.Role,
                CompanyId = u.CompanyId,
                CompanyName = u.Company != null ? u.Company.Name : null
            })
            .ToListAsync();


        // 📝 Save test users to list
        testUserStore.SetUsers(testUsers);
    }

    #endregion
}

/// @remarks
/// Developer Notes:
/// - 🚀 Seeds identity roles, users, companies, projects, screens, actions, and notes.
/// - 🧪 Faker is used for generating realistic test data (names, addresses, emails, etc.).
/// - 🔐 Multi-tenancy is respected via CompanyId and UserId in all seeded data.
/// - 🛡️ Soft delete is bypassed using IgnoreQueryFilters() during seeding.
/// - 🎯 Role-based user creation covers GlobalAdmin, Admin, Manager, and User.
/// - 💾 Test login data is saved as JSON files to simplify client-side development.
/// - ⚠️ Seeding should only be executed in development/test environments.
/// - 🧩 Consider adding localization support if Faker should adapt to UI language.
/// - 📤 Future extension: Export seeding stats, timing, or counts as structured logs or metrics.

