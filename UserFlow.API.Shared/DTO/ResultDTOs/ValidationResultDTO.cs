/// *****************************************************************************************
/// @file ValidationResultDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-27
/// @brief Reusable DTO to represent the result of a validation process.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// ✅ Represents the result of a validation check for a single record or field.
/// </summary>
public class ValidationResultDTO
{
    /// <summary>
    /// 🔎 Indicates whether the record passed validation.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 🆔 The index of the record being validated (useful in bulk operations).
    /// </summary>
    public int RecordIndex { get; set; }

    /// <summary>
    /// 🏷️ Name of the field that failed validation.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 🧩 Optional error code (e.g., "REQUIRED", "INVALID_FORMAT").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 📋 List of validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// ⚠️ Concise summary of the error (for display purposes).
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// *****************************************************************************************
/// @remarks 📄 Developer Notes:
/// - Used to return detailed validation feedback in bulk or interactive operations.
/// - `Errors` provides multiple explanations; `ErrorMessage` is a single summary string.
/// - `RecordIndex` allows mapping back to the exact row in CSV or user input.
/// - Commonly used in import/export modules and UI feedback mechanisms.
/// *****************************************************************************************
