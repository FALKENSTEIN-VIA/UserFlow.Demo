/// *****************************************************************************************
/// @file ImportErrorDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief DTO used to represent individual CSV import errors.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// ❌ DTO representing a single error that occurred during import.
/// </summary>
/// <remarks>
/// Used to communicate the row number and error message when importing data via CSV.
/// </remarks>
public class ImportErrorDTO
{
    /// <summary>
    /// 🔢 The row number in the imported CSV file where the error occurred (1-based).
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// 📝 Description of the import error.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// *****************************************************************************************
/// @remarks 🛠️ Developer Notes:
/// - Used in bulk import responses to provide error feedback for individual rows.
/// - Helps users identify and fix issues in their CSV input.
/// - Example: Row = 3, Message = "Missing required field 'Email'".
/// *****************************************************************************************
