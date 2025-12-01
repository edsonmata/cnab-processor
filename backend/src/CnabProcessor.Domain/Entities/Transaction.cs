// ========================================
// File: backend/src/CnabProcessor.Domain/Entities/Transaction.cs
// ========================================

using CnabProcessor.Domain.Enums;
using CnabProcessor.Domain.Extensions;

namespace CnabProcessor.Domain.Entities;

/// <summary>
/// Represents a financial transaction from CNAB file.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of transaction (1-9).
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Date when the transaction occurred.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Transaction amount in decimal format (already divided by 100).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Beneficiary's CPF (Brazilian tax ID).
    /// </summary>
    public string Cpf { get; set; } = string.Empty;

    /// <summary>
    /// Card number used in the transaction.
    /// </summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Time when the transaction occurred (UTC-3).
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Name of the store owner/representative.
    /// </summary>
    public string StoreOwner { get; set; } = string.Empty;

    /// <summary>
    /// Name of the store where transaction occurred.
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when record was created in database.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // ===== COMPUTED PROPERTIES =====

    /// <summary>
    /// Amount with sign applied based on transaction type.
    /// Positive for income, negative for expense.
    /// </summary>
    public decimal SignedAmount => CalculateSignedAmount();

    /// <summary>
    /// Nature of the transaction (Income or Expense).
    /// </summary>
    public TransactionNature Nature => GetTransactionNature();

    /// <summary>
    /// Human-readable description of the transaction type in English.
    /// </summary>
    public string TypeDescription => Type.GetDescription();

    /// <summary>
    /// Indicates if this is an income transaction (positive).
    /// </summary>
    public bool IsIncome => Nature == TransactionNature.Income;

    /// <summary>
    /// Indicates if this is an expense transaction (negative).
    /// </summary>
    public bool IsExpense => Nature == TransactionNature.Expense;

    // ===== METHODS =====

    /// <summary>
    /// Calculates the signed amount based on transaction type.
    /// </summary>
    private decimal CalculateSignedAmount()
    {
        return Type switch
        {
            TransactionType.Debit => Amount,         // +
            TransactionType.Boleto => -Amount,       // -
            TransactionType.Financing => -Amount,    // -
            TransactionType.Credit => Amount,        // +
            TransactionType.LoanReceipt => Amount,   // +
            TransactionType.Sales => Amount,         // +
            TransactionType.TedReceipt => Amount,    // +
            TransactionType.DocReceipt => Amount,    // +
            TransactionType.Rent => -Amount,         // -
            _ => Amount
        };
    }

    /// <summary>
    /// Determines the nature (Income/Expense) based on transaction type.
    /// </summary>
    private TransactionNature GetTransactionNature()
    {
        return Type switch
        {
            TransactionType.Debit => TransactionNature.Income,
            TransactionType.Boleto => TransactionNature.Expense,
            TransactionType.Financing => TransactionNature.Expense,
            TransactionType.Credit => TransactionNature.Income,
            TransactionType.LoanReceipt => TransactionNature.Income,
            TransactionType.Sales => TransactionNature.Income,
            TransactionType.TedReceipt => TransactionNature.Income,
            TransactionType.DocReceipt => TransactionNature.Income,
            TransactionType.Rent => TransactionNature.Expense,
            _ => TransactionNature.Income
        };
    }

    /// <summary>
    /// Validates if the transaction has all required fields.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(StoreName) &&
               !string.IsNullOrWhiteSpace(Cpf) &&
               Amount > 0 &&
               Date != default;
    }
}