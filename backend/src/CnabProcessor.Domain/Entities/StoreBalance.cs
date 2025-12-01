// ========================================
// Arquivo: CnabProcessor.Domain/Entities/StoreBalance.cs
// O que faz: Representa o saldo consolidado de uma loja
// Usado para exibir na tela
// ========================================
namespace CnabProcessor.Domain.Entities;

/// <summary>
/// Represents the consolidated balance for a store with all its transactions.
/// </summary>
public class StoreBalance
{
    /// <summary>
    /// Name of the store.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Total balance (income - expenses).
    /// </summary>
    public decimal TotalBalance { get; set; }

    /// <summary>
    /// Total income (sum of all positive transactions).
    /// </summary>
    public decimal TotalIncome { get; set; }

    /// <summary>
    /// Total expenses (sum of all negative transactions).
    /// </summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>
    /// Number of transactions for this store.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// List of all transactions for this store.
    /// </summary>
    public List<Transaction> Transactions { get; set; } = new();

    /// <summary>
    /// Calculates the balance based on the transactions list.
    /// Must be called after populating Transactions.
    /// </summary>
    public void CalculateBalance()
    {
        if (Transactions == null || !Transactions.Any())
        {
            TotalBalance = 0;
            TotalIncome = 0;
            TotalExpenses = 0;
            TransactionCount = 0;
            return;
        }

        // Calculate totals
        TotalIncome = Transactions
            .Where(t => t.IsIncome)
            .Sum(t => t.Amount);

        TotalExpenses = Transactions
            .Where(t => t.IsExpense)
            .Sum(t => t.Amount);

        // Balance = Income - Expenses
        TotalBalance = TotalIncome - TotalExpenses;

        TransactionCount = Transactions.Count;
    }
}