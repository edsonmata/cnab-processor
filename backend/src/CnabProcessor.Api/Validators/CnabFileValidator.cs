// ========================================
// File: CnabProcessor.Api/Validators/CnabFileValidator.cs
// Purpose: Validates CNAB file uploads
// ========================================

namespace CnabProcessor.Api.Validators;

/// <summary>
/// Validator for CNAB file uploads with comprehensive checks.
/// </summary>
public class CnabFileValidator
{
    // Validation constraints
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const long MinFileSizeBytes = 1; // 1 byte
    private const int ExpectedLineLength = 81; // CNAB line length

    private static readonly string[] AllowedExtensions = { ".txt", ".cnab", "" }; // Allow no extension too
    private static readonly string[] AllowedContentTypes =
    {
        "text/plain",
        "application/octet-stream",
        "application/x-cnab"
    };

    /// <summary>
    /// Validates a CNAB file upload.
    /// </summary>
    /// <param name="file">The uploaded file</param>
    /// <returns>Validation result with error messages if invalid</returns>
    public static ValidationResult Validate(IFormFile? file)
    {
        var result = new ValidationResult();

        // Check if file exists
        if (file == null)
        {
            result.AddError("No file was uploaded.");
            return result;
        }

        // Check if file is empty
        if (file.Length == 0)
        {
            result.AddError("The uploaded file is empty.");
            return result;
        }

        // Check file size - minimum
        if (file.Length < MinFileSizeBytes)
        {
            result.AddError($"File is too small. Minimum size is {MinFileSizeBytes} byte(s).");
        }

        // Check file size - maximum
        if (file.Length > MaxFileSizeBytes)
        {
            result.AddError($"File is too large. Maximum allowed size is {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            result.AddError($"Invalid file extension '{extension}'. Allowed extensions: {string.Join(", ", AllowedExtensions.Where(e => !string.IsNullOrEmpty(e)))} or no extension.");
        }

        // Check content type
        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            result.AddError($"Invalid content type '{file.ContentType}'. Allowed types: {string.Join(", ", AllowedContentTypes)}.");
        }

        // Check file name
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            result.AddError("File name is missing.");
        }
        else if (file.FileName.Length > 255)
        {
            result.AddError("File name is too long (maximum 255 characters).");
        }

        // Basic format validation (check if it looks like a CNAB file)
        if (result.IsValid)
        {
            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                var firstLine = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(firstLine))
                {
                    result.AddError("File appears to be empty or contains only whitespace.");
                }
                else
                {
                    // Check if first character is a digit (transaction type 1-9)
                    if (!char.IsDigit(firstLine[0]))
                    {
                        result.AddError("Invalid CNAB format: First character must be a transaction type (1-9).");
                    }

                    // Warn if line length is unexpected (but don't fail - parser can handle it)
                    var normalizedLength = firstLine.TrimEnd().Length;
                    if (normalizedLength != ExpectedLineLength && normalizedLength != 0)
                    {
                        // This is just a warning, don't add as error since parser normalizes lines
                        result.AddWarning($"Note: Line length is {normalizedLength} characters. Expected {ExpectedLineLength} characters. The parser will normalize this.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError($"Failed to read file content: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Validation result with errors and warnings.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public bool IsValid => _errors.Count == 0;
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();
        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        public void AddError(string message) => _errors.Add(message);
        public void AddWarning(string message) => _warnings.Add(message);

        public string GetErrorMessage() => string.Join(" ", _errors);
        public string GetWarningMessage() => string.Join(" ", _warnings);
    }
}
