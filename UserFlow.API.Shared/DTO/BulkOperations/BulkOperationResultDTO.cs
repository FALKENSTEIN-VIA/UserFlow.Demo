/// *****************************************************************************************
/// @file BulkOperationResultDTO.cs
/// @author Claus Falkenstein
/// @company VIA Software GmbH
/// @date 2025-04-26
/// @brief DTOs for representing the result of bulk operations, including success statistics and errors.
/// *****************************************************************************************

namespace UserFlow.API.Shared.DTO;

/// <summary>
/// 📊 Represents the result of a bulk or import operation.
/// </summary>
/// <typeparam name="T">The type of successfully imported/processed items.</typeparam>
/// <remarks>
/// This DTO is used to report the outcome of a batch operation (e.g., import, bulk create),
/// including how many rows were processed successfully and which errors occurred.
/// </remarks>
public class BulkOperationResultDTO<T>
{
    /// <summary>
    /// 🔢 Total number of rows attempted (success + errors).
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// ✅ Number of rows successfully processed/imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// ❌ List of errors that occurred during processing.
    /// </summary>
    public List<BulkOperationErrorDTO> Errors { get; set; } = new();

    /// <summary>
    /// 📦 List of successfully processed entities.
    /// </summary>
    public List<T> Items { get; set; } = [];
}

/// <summary>
/// ❗ Represents detailed information about a single error in a bulk operation.
/// </summary>
/// <remarks>
/// Each error entry provides information such as the index of the failing record,
/// the field and error code involved, and optional raw values of the record.
/// </remarks>
public class BulkOperationErrorDTO
{
    /// <summary>
    /// 🔧 Default constructor.
    /// </summary>
    public BulkOperationErrorDTO() { }

    /// <summary>
    /// 🛠️ Initializes a new error record with details.
    /// </summary>
    /// <param name="recordIndex">Zero-based index of the failing row.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="field">Field where the error occurred (optional).</param>
    /// <param name="code">Error code identifier.</param>
    public BulkOperationErrorDTO(int recordIndex, string message, string? field = null, string code = "")
    {
        RecordIndex = recordIndex;
        Message = message;
        Field = field;
        Code = code;
    }

    /// <summary>
    /// 🔢 Index of the erroneous record (0-based).
    /// </summary>
    public int RecordIndex { get; set; }

    /// <summary>
    /// 🏷️ Optional name of the field causing the error.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// 🔐 Error code to identify the type of issue (e.g., "REQUIRED_FIELD").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 📄 Human-readable explanation of the error.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 📂 Optional dictionary of original values from the faulty input row.
    /// </summary>
    public Dictionary<string, object>? Values { get; set; }
}

/// @remarks
/// 🛠️ **Developer Notes**
/// - `BulkOperationResultDTO<T>` is used for batch operations such as CSV import, bulk insert, etc.
/// - `Errors` contains detailed diagnostics for failed records, including optional original data for debugging.
/// - `Items` holds successfully processed objects of type `T`.
/// - Can be used as a generic response from endpoints like `POST /import`, `POST /bulk`, etc.
///
/// 🧪 **Example Response**
/// ```json
/// {
///   "totalRows": 10,
///   "importedCount": 7,
///   "errors": [
///     {
///       "recordIndex": 2,
///       "field": "Email",
///       "code": "INVALID_FORMAT",
///       "message": "The email address is invalid.",
///       "values": {
///         "Name": "John Doe",
///         "Email": "invalid-email"
///       }
///     }
///   ],
///   "items": [ ... ]
/// }
/// ```
/// *****************************************************************************************
