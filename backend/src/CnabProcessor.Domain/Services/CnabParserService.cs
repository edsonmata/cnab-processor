// ========================================
// File: CnabProcessor.Domain/Services/CnabParserService.cs
// Purpose: Service for parsing CNAB fixed-width text files
// ========================================

using System.Globalization;
using System.Text;
using CnabProcessor.Domain.Entities;
using CnabProcessor.Domain.Enums;
using CnabProcessor.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CnabProcessor.Domain.Services;

/// <summary>
/// Service responsible for parsing CNAB (Centro Nacional de Automação Bancária) files.
/// CNAB is a Brazilian standard for financial transaction files with fixed-width format.
/// </summary>
public class CnabParserService : ICnabParser
{
    private readonly ILogger<CnabParserService> _logger;
    private const int ExpectedLineLength = 81;

    // CNAB field positions (0-based indexing)
    private const int TypePosition = 0;      // 1 character
    private const int DatePosition = 1;      // 8 characters (yyyyMMdd)
    private const int AmountPosition = 9;    // 10 characters (cents)
    private const int CpfPosition = 19;      // 11 characters
    private const int CardPosition = 30;     // 12 characters
    private const int TimePosition = 42;     // 6 characters (HHmmss)
    private const int OwnerPosition = 48;    // 14 characters
    private const int StorePosition = 62;    // 19 characters

    /// <summary>
    /// Initializes a new instance of the CnabParserService.
    /// </summary>
    /// <param name="logger">Logger for tracking parsing operations</param>
    public CnabParserService(ILogger<CnabParserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses a CNAB file stream synchronously and extracts transactions.
    /// </summary>
    /// <param name="fileStream">Stream containing the CNAB file</param>
    /// <returns>Collection of parsed transactions</returns>
    public IEnumerable<Transaction> Parse(Stream fileStream)
    {
        var transactions = new List<Transaction>();
        fileStream.Position = 0;

        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        string? line;
        var lineNumber = 0;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            line = NormalizeLine(line);

            try
            {
                var transaction = ParseLine(line);

                // Validate transaction before adding
                if (transaction.IsValid())
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue processing
                _logger.LogWarning(ex, "Failed to parse line {LineNumber}: {ErrorMessage}", lineNumber, ex.Message);
            }
        }

        return transactions;
    }

    /// <summary>
    /// Parses a CNAB file stream asynchronously and extracts transactions.
    /// Recommended for large files or I/O-bound operations.
    /// </summary>
    /// <param name="fileStream">Stream containing the CNAB file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of parsed transactions</returns>
    public async Task<IEnumerable<Transaction>> ParseAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        var transactions = new List<Transaction>();
        fileStream.Position = 0;

        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        string? line;
        var lineNumber = 0;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            line = NormalizeLine(line);

            try
            {
                var transaction = ParseLine(line);

                if (transaction.IsValid())
                {
                    transactions.Add(transaction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse line {LineNumber}: {ErrorMessage}", lineNumber, ex.Message);
            }
        }

        return transactions;
    }

    /// <summary>
    /// Normalizes line length to expected format.
    /// Pads short lines with spaces, truncates long lines.
    /// </summary>
    private static string NormalizeLine(string line)
    {
        if (line.Length < ExpectedLineLength)
            return line.PadRight(ExpectedLineLength, ' ');

        if (line.Length > ExpectedLineLength)
            return line.Substring(0, ExpectedLineLength);

        return line;
    }

    /// <summary>
    /// Parses a single CNAB line into a Transaction entity.
    /// </summary>
    private Transaction ParseLine(string line)
    {
        // Parse transaction type
        var typeString = line.Substring(TypePosition, 1);

        if (!int.TryParse(typeString, out var type) || type < 1 || type > 9)
            throw new FormatException("Invalid transaction type");

        // Parse date (yyyyMMdd format)
        var date = DateTime.ParseExact(
            line.Substring(DatePosition, 8),
            "yyyyMMdd",
            CultureInfo.InvariantCulture);

        // Parse amount (value in cents, needs to be divided by 100)
        var rawCents = long.Parse(line.Substring(AmountPosition, 10));
        var amount = rawCents / 100m;

        // Parse and clean CPF (remove non-numeric characters)
        var cpf = ExtractDigits(line.Substring(CpfPosition, 11));

        // Parse card number
        var card = line.Substring(CardPosition, 12).Trim();

        // Parse time (HHmmss format)
        var time = ParseTime(line.Substring(TimePosition, 6));

        // Parse store owner and store name
        var owner = line.Substring(OwnerPosition, 14).Trim();
        var store = line.Substring(StorePosition, 19).Trim();

        return new Transaction
        {
            Type = (TransactionType)type,
            Date = date,
            Time = time,
            Amount = amount,
            Cpf = cpf,
            CardNumber = card,
            StoreOwner = owner,
            StoreName = store,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Parses time string in HHmmss format to TimeSpan.
    /// </summary>
    private static TimeSpan ParseTime(string hhmmss)
    {
        return TimeSpan.ParseExact(hhmmss, "hhmmss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Extracts only numeric digits from a string.
    /// Useful for cleaning CPF and other numeric fields.
    /// </summary>
    private static string ExtractDigits(string input)
    {
        return new string(input.Where(char.IsDigit).ToArray());
    }
}