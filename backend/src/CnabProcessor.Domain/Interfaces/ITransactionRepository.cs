// ========================================
// File: CnabProcessor.Infrastructure/Interfaces/ITransactionRepository.cs
// Purpose: Repository interface for Transaction operations
// ========================================

using CnabProcessor.Domain.Entities;

namespace CnabProcessor.Infrastructure.Interfaces;

/// <summary>
/// Repository interface for Transaction entity operations.
/// Defines data access contract.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Retrieves all transactions.
    /// </summary>
    Task<IEnumerable<Transaction>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves transactions for a specific store.
    /// </summary>
    Task<IEnumerable<Transaction>> GetByStoreAsync(string storeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregated store balances.
    /// </summary>
    Task<IEnumerable<StoreBalance>> GetStoreBalancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a single transaction.
    /// </summary>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple transactions.
    /// </summary>
    Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);

    /// <summary>
    /// OPTIMIZED bulk insert for large datasets (10k+ records).
    /// Uses batching and disables change tracking for maximum performance.
    /// 🚀 Up to 10x faster than AddRangeAsync!
    /// </summary>
    /// <param name="transactions">Transactions to insert</param>
    /// <param name="batchSize">Records per batch (default: 5000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records inserted</returns>
    Task<int> BulkInsertAsync(
        IEnumerable<Transaction> transactions,
        int batchSize = 5000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any transactions exist.
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total count of transactions.
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all transactions (use with caution).
    /// </summary>
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}