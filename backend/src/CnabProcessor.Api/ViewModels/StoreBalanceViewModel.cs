// ========================================
// File: backend/src/CnabProcessor.Api/ViewModels/StoreBalanceViewModel.cs
// ========================================

using System.Collections.Generic;

namespace CnabProcessor.Api.ViewModels;

/// <summary>
/// View model for store balance aggregation.
/// </summary>
public class StoreBalanceViewModel
{
    /// <summary>
    /// Name of the store.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Total balance for the store (income - expenses).
    /// </summary>
    public decimal TotalBalance { get; set; }

    /// <summary>
    /// Total income for the store.
    /// </summary>
    public decimal TotalIncome { get; set; }

    /// <summary>
    /// Total expenses for the store.
    /// </summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>
    /// Number of transactions for this store.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// List of all transactions for this store.
    /// </summary>
    public List<TransactionViewModel> Transactions { get; set; } = new();
}