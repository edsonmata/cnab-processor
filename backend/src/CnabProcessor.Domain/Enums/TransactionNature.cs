// ========================================
// File: CnabProcessor.Domain/Enums/TransactionNature.cs
// Purpose: Enum for transaction nature (Income/Expense)
// ========================================

using System.ComponentModel;

namespace CnabProcessor.Domain.Enums;

/// <summary>
/// Represents the nature of a financial transaction.
/// </summary>
public enum TransactionNature
{
    /// <summary>
    /// Income transaction (positive impact on balance).
    /// </summary>
    [Description("Income")]
    Income,

    /// <summary>
    /// Expense transaction (negative impact on balance).
    /// </summary>
    [Description("Expense")]
    Expense
}