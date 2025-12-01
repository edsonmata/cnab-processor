// ========================================
// File: CnabProcessor.Domain/Enums/TransactionType.cs
// Purpose: Enum defining CNAB transaction types
// ========================================

using System.ComponentModel;

namespace CnabProcessor.Domain.Enums;

/// <summary>
/// Types of financial transactions in CNAB format.
/// Each type has a specific nature (income or expense).
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Debit transaction (income)
    /// </summary>
    [Description("Debit")]
    Debit = 1,

    /// <summary>
    /// Boleto payment (expense)
    /// </summary>
    [Description("Boleto Payment")]
    Boleto = 2,

    /// <summary>
    /// Financing payment (expense)
    /// </summary>
    [Description("Financing")]
    Financing = 3,

    /// <summary>
    /// Credit transaction (income)
    /// </summary>
    [Description("Credit")]
    Credit = 4,

    /// <summary>
    /// Loan receipt (income)
    /// </summary>
    [Description("Loan Receipt")]
    LoanReceipt = 5,

    /// <summary>
    /// Sales revenue (income)
    /// </summary>
    [Description("Sales")]
    Sales = 6,

    /// <summary>
    /// TED receipt (income)
    /// </summary>
    [Description("TED Receipt")]
    TedReceipt = 7,

    /// <summary>
    /// DOC receipt (income)
    /// </summary>
    [Description("DOC Receipt")]
    DocReceipt = 8,

    /// <summary>
    /// Rent payment (expense)
    /// </summary>
    [Description("Rent")]
    Rent = 9
}