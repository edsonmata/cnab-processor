// ========================================
// File: backend/src/CnabProcessor.Api/ViewModels/UploadResponseViewModel.cs
// ========================================

namespace CnabProcessor.Api.ViewModels;

/// <summary>
/// Response model for file upload operations.
/// </summary>
public class UploadResponseViewModel
{
    /// <summary>
    /// Indicates if the upload was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Number of transactions successfully imported.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Original filename that was uploaded.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}