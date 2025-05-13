/// @file SeederOptions.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Configuration options for controlling the amount and structure of seed data.
/// @details
/// Provides flexible parameters to define how much test data is generated during database seeding.
/// Supports configuration of users, projects per user, screens per project, actions per screen, and notes.
/// Can be passed to the DataSeeder to adjust the generated data volume for different environments
/// (e.g., development, testing, staging).


namespace UserFlow.API.Data;

/// <summary>
/// 👉 ✨ Options to customize how much data the DataSeeder generates.
/// </summary>
public static class DataSeederOptions
{
    private static readonly Random rand = new(Guid.NewGuid().GetHashCode());

    /// <summary>
    /// 👉 ✨ Number of test users to generate (excluding admin user).
    /// </summary>
    public static int CompaniesCount { get; set; } = 20;

    /// <summary>
    /// 👉 ✨ Number of managers to generate 
    /// </summary>
    public static int ManagersCount { get; set; } = rand.Next(1) + 1;

    /// <summary>
    /// 👉 ✨ Number of test users to generate (excluding admin user).
    /// </summary>
    public static int UserCount { get; set; } = rand.Next(25) + 10;

    /// <summary>
    /// 👉 ✨ Range of projects to create per user (min, max).
    /// </summary>
    public static int ProjectsPerUser { get; set; } = rand.Next(10) + 2;

    /// <summary>
    /// 👉 ✨ Range of screens to create per project (min, max).
    /// </summary>
    public static int ScreensPerProject { get; set; } = rand.Next(8) + 2;

}

/// @remarks
/// Developer Notes:
/// - 🧩 `DataSeederOptions` enables flexible and structured test data generation.
/// - 🎛️ Fully customizable ranges for each seeding dimension (users, projects, screens, etc.).
/// - 🔁 Ideal for adapting seed data across different environments (dev, test, staging).
/// - 💡 Defaults are balanced for fast local development.
/// - ⚙️ Optional: pass as parameter into `DataSeeder.Seed(context, options)` to override defaults.
/// - 🧪 Useful for simulating high-load or multi-user scenarios in test cases.
