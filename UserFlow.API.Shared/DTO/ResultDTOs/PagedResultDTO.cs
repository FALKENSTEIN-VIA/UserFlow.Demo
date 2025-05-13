/// *****************************************************************************************
/// @file PagedResultDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Generic DTO for paginated API responses.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 📦 Generic data transfer object for paginated result sets.
/// </summary>
/// <typeparam name="T">The type of items returned in the paginated response.</typeparam>
public class PagedResultDTO<T>
{
    /// <summary>
    /// 🔢 Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 📏 Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// ✅ Number of successfully imported or retrieved items (optional usage).
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// 📄 List of items on the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - This DTO is used for any endpoint that returns paginated data.
/// - `ImportedCount` can be used optionally to indicate successful imports in paged imports.
/// - Default initialization ensures Items is never null.
/// - Often combined with filters and sorting parameters in query requests.
/// *****************************************************************************************
