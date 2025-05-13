/// @file AppDbContext.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Entity Framework Core database context with global query filters for SoftDelete and Multi-Tenancy.
/// @details
/// The AppDbContext manages all database entities for the UserFlow API.
/// It applies global query filters to automatically enforce multi-tenancy (by UserId)
/// and soft delete behavior (IsDeleted), ensuring data isolation and lifecycle control.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserFlow.API.Data.Configurations;
using UserFlow.API.Data.Entities;
using UserFlow.API.Services.Interfaces;

namespace UserFlow.API.Data;

/// <summary>
/// 👉 ✨ EF Core database context with Identity and global filters for soft delete and multi-tenancy.
/// </summary>
public class AppDbContext : IdentityDbContext<User, IdentityRole<long>, long>
{
    /// 🧩 Injected service for accessing the current user's ID
    private readonly ICurrentUserService _currentUserService;

    /// 📋 Logger for debug or lifecycle events
    private readonly ILogger<AppDbContext> _logger;

    /// <summary>
    /// 👷 Constructor accepting DbContext options, current user service, and logger.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService, ILogger<AppDbContext> logger)
        : base(options)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// 🔧 Called during model creation to apply configurations and global filters.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /// 🔗 Apply entity configurations
        modelBuilder.ApplyConfiguration(new AppUserConfiguration());
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectConfiguration());
        modelBuilder.ApplyConfiguration(new ScreenConfiguration());
        modelBuilder.ApplyConfiguration(new ScreenActionConfiguration());
        modelBuilder.ApplyConfiguration(new ScreenActionTypeConfiguration());
        modelBuilder.ApplyConfiguration(new NoteConfiguration());

        /// 🛡 Apply static global query filters for soft delete (IsDeleted)
        _logger.LogInformation("👉 ✨ Global Filters \"IsDeleted\" have been applied for all Entities..." + Environment.NewLine);

        modelBuilder.Entity<Company>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<Screen>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<ScreenAction>().HasQueryFilter(sa => !sa.IsDeleted);
        modelBuilder.Entity<ScreenActionType>().HasQueryFilter(sat => !sat.IsDeleted);
        modelBuilder.Entity<Note>().HasQueryFilter(n => !n.IsDeleted);
    }

    /// <summary>
    /// 💾 Overrides EF Core's SaveChangesAsync to add audit information.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                /// 🕓 Set creation and modification timestamps on insert
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                /// ✏️ Update modification timestamp and user on update
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    #region 👉 ✨ DbSets (Tables)

    /// <summary> 🔄 Refresh tokens used for JWT authentication. </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary> 🏢 Companies (tenants). </summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <summary> 👥 Employee records (e.g., team members). </summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary> 📁 User-owned projects. </summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary> 🖼 Screens within projects. </summary>
    public DbSet<Screen> Screens => Set<Screen>();

    /// <summary> 🎯 User actions triggered on screens. </summary>
    public DbSet<ScreenAction> ScreenActions => Set<ScreenAction>();

    /// <summary> 🏷 Types of screen actions. </summary>
    public DbSet<ScreenActionType> ScreenActionTypes => Set<ScreenActionType>();

    /// <summary> 📝 Notes attached to screens or projects. </summary>
    public DbSet<Note> Notes => Set<Note>();



    #endregion
}

