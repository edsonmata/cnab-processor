// ========================================
// File: CnabProcessor.Domain/Interfaces/ICnabParser.cs
// Purpose: Parser interface for CNAB file operations
// ========================================

using CnabProcessor.Domain.Entities;

namespace CnabProcessor.Domain.Interfaces;

/// <summary>
/// Interface for CNAB file parsing operations.
/// </summary>
public interface ICnabParser
{
    /// <summary>
    /// Parses CNAB file synchronously.
    /// </summary>
    /// <param name="fileStream">Stream containing CNAB data</param>
    /// <returns>Collection of parsed transactions</returns>
    IEnumerable<Transaction> Parse(Stream fileStream);

    /// <summary>
    /// Parses CNAB file asynchronously.
    /// </summary>
    /// <param name="fileStream">Stream containing CNAB data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of parsed transactions</returns>
    Task<IEnumerable<Transaction>> ParseAsync(Stream fileStream, CancellationToken cancellationToken = default);
}