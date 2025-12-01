// ========================================
// File: CnabProcessor.UnitTests/CnabParserServiceTests.cs
// Purpose: Unit tests for CNAB parsing service
// ========================================

using CnabProcessor.Domain.Enums;
using CnabProcessor.Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CnabProcessor.UnitTests;

/// <summary>
/// Unit tests for CnabParserService.
/// Tests parsing logic in isolation.
/// </summary>
public class CnabParserServiceTests
{
    private readonly CnabParserService _parser;
    private readonly Mock<ILogger<CnabParserService>> _loggerMock;

    public CnabParserServiceTests()
    {
        _loggerMock = new Mock<ILogger<CnabParserService>>();
        _parser = new CnabParserService(_loggerMock.Object);
    }

    #region Parse Tests

    [Fact]
    public void Parse_ValidSingleLine_ReturnsTransaction()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        Assert.Single(transactions);

        var transaction = transactions.First();
        Assert.Equal(TransactionType.Financing, transaction.Type);
        Assert.Equal(new DateTime(2019, 3, 1), transaction.Date);
        Assert.Equal(142.00m, transaction.Amount);
        Assert.Equal("09620676017", transaction.Cpf);
        Assert.Equal("4753****3153", transaction.CardNumber);
        Assert.Equal(new TimeSpan(14, 13, 58), transaction.Time);
        Assert.Equal("JOÃO MACEDO", transaction.StoreOwner);
        Assert.Equal("BAR DO JOÃO", transaction.StoreName);
    }

    [Fact]
    public void Parse_MultipleLines_ReturnsAllTransactions()
    {
        // Arrange
        var content = string.Join("\n", new[]
        {
            "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ",
            "5201903010000013200556418150633648****0099153453MARIA SILVA   MERCEARIA 3 IRMÃOS",
            "2201903010000012200845152540736777****1313172712MARCOS PEREIRA LOJA DO Ó - MATRIZ"
        });
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        Assert.Equal(3, transactions.Count);

        // Verify each transaction type
        Assert.Equal(TransactionType.Financing, transactions[0].Type);
        Assert.Equal(TransactionType.LoanReceipt, transactions[1].Type);
        Assert.Equal(TransactionType.Boleto, transactions[2].Type);
    }

    [Fact]
    public void Parse_AllTransactionTypes_ParsesCorrectly()
    {
        // Arrange - Create line for each transaction type (1-9)
        var lines = new List<string>
        {
            "1201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ", // Debit
            "2201903010000013200556418150633648****0099153453MARIA SILVA   MERCEARIA 3 IRMÃOS", // Boleto
            "3201903010000012200845152540736777****1313172712MARCOS PEREIRA LOJA DO Ó - MATRIZ", // Financing
            "4201903010000015000876953201231234****0987162300JOSÉ ROBERTO  POSTO PETROBRÁS   ", // Credit
            "5201903010000016000998877665544321****1234122345ANA PAULA     FARMÁCIA SAÚDE    ", // LoanReceipt
            "6201903010000017000112233445556789****9876133210PEDRO SANTOS  SUPERMERCADO BOM  ", // Sales
            "7201903010000018000223344556667890****8765144321LUCAS OLIVEIRA LOJA DE ROUPAS   ", // TedReceipt
            "8201903010000019000334455667778901****7654155432JULIANA COSTA PAPELARIA ESCOLAR", // DocReceipt
            "9201903010000020000445566778889012****6543161543ROBERTO SILVA IMOBILIÁRIA CASA "  // Rent
        };
        var content = string.Join("\n", lines);
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        Assert.Equal(9, transactions.Count);

        // Verify all types are present
        Assert.Contains(transactions, t => t.Type == TransactionType.Debit);
        Assert.Contains(transactions, t => t.Type == TransactionType.Boleto);
        Assert.Contains(transactions, t => t.Type == TransactionType.Financing);
        Assert.Contains(transactions, t => t.Type == TransactionType.Credit);
        Assert.Contains(transactions, t => t.Type == TransactionType.LoanReceipt);
        Assert.Contains(transactions, t => t.Type == TransactionType.Sales);
        Assert.Contains(transactions, t => t.Type == TransactionType.TedReceipt);
        Assert.Contains(transactions, t => t.Type == TransactionType.DocReceipt);
        Assert.Contains(transactions, t => t.Type == TransactionType.Rent);
    }

    [Fact]
    public void Parse_AmountInCents_ConvertsToDecimal()
    {
        // Arrange - Amount is 0000014200 cents = 142.00
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        var transaction = transactions.First();
        Assert.Equal(142.00m, transaction.Amount);
    }

    [Fact]
    public void Parse_BlankLines_AreIgnored()
    {
        // Arrange
        var content = string.Join("\n", new[]
        {
            "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ",
            "",
            "   ",
            "5201903010000013200556418150633648****0099153453MARIA SILVA   MERCEARIA 3 IRMÃOS"
        });
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        Assert.Equal(2, transactions.Count);
    }

    [Fact]
    public void Parse_TrimmedFields_HandlesCorrectly()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO          BAR               ";
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        var transaction = transactions.First();
        Assert.Equal("JOÃO", transaction.StoreOwner);
        Assert.Equal("BAR", transaction.StoreName);
    }

    [Fact]
    public void Parse_DifferentDates_ParsesCorrectly()
    {
        // Arrange
        var content = string.Join("\n", new[]
        {
            "3201901150000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ", // Jan 15
            "3201906220000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ", // Jun 22
            "3201912310000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       "  // Dec 31
        });
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        Assert.Equal(new DateTime(2019, 1, 15), transactions[0].Date);
        Assert.Equal(new DateTime(2019, 6, 22), transactions[1].Date);
        Assert.Equal(new DateTime(2019, 12, 31), transactions[2].Date);
    }

    [Fact]
    public void Parse_DifferentTimes_ParsesCorrectly()
    {
        // Arrange
        var content = string.Join("\n", new[]
        {
            "3201903010000014200096206760174753****3153000000JOÃO MACEDO   BAR DO JOÃO       ", // 00:00:00
            "3201903010000014200096206760174753****3153123045JOÃO MACEDO   BAR DO JOÃO       ", // 12:30:45
            "3201903010000014200096206760174753****3153235959JOÃO MACEDO   BAR DO JOÃO       "  // 23:59:59
        });
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        Assert.Equal(new TimeSpan(0, 0, 0), transactions[0].Time);
        Assert.Equal(new TimeSpan(12, 30, 45), transactions[1].Time);
        Assert.Equal(new TimeSpan(23, 59, 59), transactions[2].Time);
    }

    #endregion

    #region ParseAsync Tests

    [Fact]
    public async Task ParseAsync_ValidFile_ReturnsTransactions()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = CreateStream(content);

        // Act
        var transactions = (await _parser.ParseAsync(stream)).ToList();

        // Assert
        Assert.Single(transactions);
        Assert.Equal(TransactionType.Financing, transactions.First().Type);
    }

    [Fact]
    public async Task ParseAsync_WithCancellationToken_Cancels()
    {
        // Arrange
        var content = string.Join("\n", Enumerable.Repeat(
            "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ", 1000));
        var stream = CreateStream(content);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await _parser.ParseAsync(stream, cts.Token);
        });
    }

    #endregion

    #region Transaction Validation Tests

    [Fact]
    public void Parse_ValidTransaction_IsValidReturnsTrue()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = CreateStream(content);

        // Act
        var transactions = _parser.Parse(stream).ToList();

        // Assert
        Assert.All(transactions, t => Assert.True(t.IsValid()));
    }

    [Fact]
    public void Parse_TransactionProperties_AreSetCorrectly()
    {
        // Arrange
        var content = "1201903010000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = CreateStream(content);

        // Act
        var transaction = _parser.Parse(stream).First();

        // Assert - Computed properties
        Assert.Equal(142.00m, transaction.SignedAmount); // Debit is positive
        Assert.Equal(TransactionNature.Income, transaction.Nature);
        Assert.True(transaction.IsIncome);
        Assert.False(transaction.IsExpense);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_LongStoreName_TruncatesCorrectly()
    {
        // Arrange - Store name field is 19 chars, test with longer name
        var content = "3201903010000014200096206760174753****3153141358JOÃO MACEDO   SUPERMERCADO MUITO GRANDE LTDA";
        var stream = CreateStream(content);

        // Act
        var transaction = _parser.Parse(stream).First();

        // Assert
        Assert.NotNull(transaction.StoreName);
        Assert.True(transaction.StoreName.Length <= 19);
    }

    [Fact]
    public void Parse_SpecialCharactersInNames_HandlesCorrectly()
    {
        // Arrange
        var content = "3201903010000014200096206760174753****3153141358JOSÉ SILVA    LOJA DO Ó - FILIAL";
        var stream = CreateStream(content);

        // Act
        var transaction = _parser.Parse(stream).First();

        // Assert
        Assert.Contains("JOSÉ", transaction.StoreOwner);
        Assert.Contains("Ó", transaction.StoreName);
    }

    [Fact]
    public void Parse_LargeAmount_ParsesCorrectly()
    {
        // Arrange - 9999999999 cents = 99,999,999.99
        var content = "3201903019999999999096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = CreateStream(content);

        // Act
        var transaction = _parser.Parse(stream).First();

        // Assert
        Assert.Equal(99999999.99m, transaction.Amount);
    }

    [Fact]
    public void Parse_MinimalAmount_ParsesCorrectly()
    {
        // Arrange - 1 cent = 0.01
        var content = "3201903010000000001096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ";
        var stream = CreateStream(content);

        // Act
        var transaction = _parser.Parse(stream).First();

        // Assert
        Assert.Equal(0.01m, transaction.Amount);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a memory stream from string content.
    /// </summary>
    private static Stream CreateStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    #endregion
}