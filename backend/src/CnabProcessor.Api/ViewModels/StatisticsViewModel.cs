// ========================================
// File: backend/src/CnabProcessor.Api/ViewModels/StatisticsViewModel.cs
// ========================================

namespace CnabProcessor.Api.ViewModels;

/// <summary>
/// View model for overall system statistics.
/// </summary>
public class StatisticsViewModel
{
    /// <summary>
    /// Total number of transactions in the system.
    /// </summary>
    public int TotalTransactions { get; set; }

    /// <summary>
    /// Total number of distinct stores.
    /// </summary>
    public int TotalStores { get; set; }

    /// <summary>
    /// Overall system balance (sum of all store balances).
    /// </summary>
    public decimal TotalBalance { get; set; }

    /// <summary>
    /// Name of the store with the highest balance.
    /// </summary>
    public string? BiggestStore { get; set; }

    /// <summary>
    /// Name of the store with the lowest balance.
    /// </summary>
    public string? SmallestStore { get; set; }
}