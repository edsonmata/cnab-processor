// ========================================
// File: backend/src/CnabProcessor.Api/ViewModels/TransactionViewModel.cs
// ========================================

using System;

namespace CnabProcessor.Api.ViewModels;

/// <summary>
/// View model for transaction data transfer to frontend.
/// </summary>
public class TransactionViewModel
{
    public int Id { get; set; }

    /// <summary>
    /// Transaction type number (1-9) as string.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of transaction type in Portuguese.
    /// </summary>
    public string TypeDescription { get; set; } = string.Empty;

    /// <summary>
    /// Nature of transaction: "Income" or "Expense".
    /// </summary>
    public string Nature { get; set; } = string.Empty;

    /// <summary>
    /// Date when the transaction occurred.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Time when the transaction occurred (formatted as string HH:mm:ss).
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount (always positive).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Amount with sign applied (positive for income, negative for expense).
    /// </summary>
    public decimal SignedAmount { get; set; }

    /// <summary>
    /// Beneficiary's CPF.
    /// </summary>
    public string Cpf { get; set; } = string.Empty;

    /// <summary>
    /// Card number used in transaction.
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Store owner name.
    /// </summary>
    public string StoreOwner { get; set; } = string.Empty;

    /// <summary>
    /// Store name.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;
}