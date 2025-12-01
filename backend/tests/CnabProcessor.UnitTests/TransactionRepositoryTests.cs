// ========================================
// File: CnabProcessor.UnitTests/TransactionRepositoryTests.cs
// Purpose: Unit tests for Transaction Repository
// ========================================

using CnabProcessor.Domain.Entities;
using CnabProcessor.Domain.Enums;
using CnabProcessor.Infrastructure.Data;
using CnabProcessor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CnabProcessor.UnitTests;

/// <summary>
/// Unit tests for TransactionRepository.
/// Uses in-memory database for isolation.
/// </summary>
public class TransactionRepositoryTests : IDisposable
{
    private readonly CnabDbContext _context;
    private readonly TransactionRepository _repository;
    private readonly Mock<ILogger<TransactionRepository>> _loggerMock;

    public TransactionRepositoryTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<CnabDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CnabDbContext(options);
        _loggerMock = new Mock<ILogger<TransactionRepository>>();
        _repository = new TransactionRepository(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidTransaction_AddsSuccessfully()
    {
        // Arrange
        var transaction = CreateValidTransaction();

        // Act
        await _repository.AddAsync(transaction);
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddAsync_NullTransaction_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_InvalidTransaction_ThrowsArgumentException()
    {
        // Arrange - Create invalid transaction (empty store name)
        var transaction = CreateValidTransaction();
        transaction.StoreName = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _repository.AddAsync(transaction));
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_ValidTransactions_AddsSuccessfully()
    {
        // Arrange
        var transactions = new[]
        {
            CreateValidTransaction("Store 1"),
            CreateValidTransaction("Store 2"),
            CreateValidTransaction("Store 3")
        };

        // Act
        await _repository.AddRangeAsync(transactions);
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AddRangeAsync_NullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.AddRangeAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_EmptyCollection_DoesNothing()
    {
        // Arrange
        var transactions = Array.Empty<Transaction>();

        // Act
        await _repository.AddRangeAsync(transactions);
        await _repository.SaveChangesAsync();

        // Assert
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task AddRangeAsync_MixedValidAndInvalid_AddsOnlyValid()
    {
        // Arrange
        var invalidTransaction = CreateValidTransaction();
        invalidTransaction.StoreName = ""; // Make invalid

        var transactions = new[]
        {
            CreateValidTransaction("Store 1"),
            invalidTransaction,
            CreateValidTransaction("Store 2")
        };

        // Act
        await _repository.AddRangeAsync(transactions);
        await _repository.SaveChangesAsync();

        // Assert - Only 2 valid transactions should be added
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(2, count);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithData_ReturnsAllTransactions()
    {
        // Arrange
        await SeedTransactions(5);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(5, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDateDescending()
    {
        // Arrange
        var transaction1 = CreateValidTransaction();
        transaction1.Date = new DateTime(2023, 1, 1);
        transaction1.Time = new TimeSpan(10, 0, 0);

        var transaction2 = CreateValidTransaction();
        transaction2.Date = new DateTime(2023, 1, 2);
        transaction2.Time = new TimeSpan(10, 0, 0);

        await _context.Transactions.AddRangeAsync(transaction1, transaction2);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAllAsync()).ToList();

        // Assert - Most recent date should be first
        Assert.Equal(new DateTime(2023, 1, 2), result[0].Date);
        Assert.Equal(new DateTime(2023, 1, 1), result[1].Date);
    }

    #endregion

    #region GetByStoreAsync Tests

    [Fact]
    public async Task GetByStoreAsync_ExistingStore_ReturnsTransactions()
    {
        // Arrange
        await SeedTransactions(3, "Store A");
        await SeedTransactions(2, "Store B");

        // Act
        var result = await _repository.GetByStoreAsync("Store A");

        // Assert
        Assert.Equal(3, result.Count());
        Assert.All(result, t => Assert.Equal("Store A", t.StoreName));
    }

    [Fact]
    public async Task GetByStoreAsync_NonExistentStore_ReturnsEmpty()
    {
        // Arrange
        await SeedTransactions(3, "Store A");

        // Act
        var result = await _repository.GetByStoreAsync("Store Z");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByStoreAsync_NullStoreName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.GetByStoreAsync(null!));
    }

    [Fact]
    public async Task GetByStoreAsync_EmptyStoreName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.GetByStoreAsync(""));
    }

    #endregion

    #region GetStoreBalancesAsync Tests

    [Fact]
    public async Task GetStoreBalancesAsync_EmptyDatabase_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetStoreBalancesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStoreBalancesAsync_MultipleStores_ReturnsBalances()
    {
        // Arrange
        await SeedTransactions(2, "Store A");
        await SeedTransactions(3, "Store B");

        // Act
        var result = (await _repository.GetStoreBalancesAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, b => b.StoreName == "Store A" && b.TransactionCount == 2);
        Assert.Contains(result, b => b.StoreName == "Store B" && b.TransactionCount == 3);
    }

    [Fact]
    public async Task GetStoreBalancesAsync_CalculatesBalancesCorrectly()
    {
        // Arrange - Add income and expense transactions
        var incomeTransaction = CreateValidTransaction("Store A");
        incomeTransaction.Type = TransactionType.Credit; // Income
        incomeTransaction.Amount = 100.00m;

        var expenseTransaction = CreateValidTransaction("Store A");
        expenseTransaction.Type = TransactionType.Boleto; // Expense
        expenseTransaction.Amount = 30.00m;

        await _context.Transactions.AddRangeAsync(incomeTransaction, expenseTransaction);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetStoreBalancesAsync()).ToList();

        // Assert
        var storeBalance = result.First();
        Assert.Equal(100.00m, storeBalance.TotalIncome);
        Assert.Equal(30.00m, storeBalance.TotalExpenses);
        Assert.Equal(70.00m, storeBalance.TotalBalance); // 100 - 30
    }

    #endregion

    #region AnyAsync and CountAsync Tests

    [Fact]
    public async Task AnyAsync_EmptyDatabase_ReturnsFalse()
    {
        // Act
        var result = await _repository.AnyAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AnyAsync_WithData_ReturnsTrue()
    {
        // Arrange
        await SeedTransactions(1);

        // Act
        var result = await _repository.AnyAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await SeedTransactions(7);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        Assert.Equal(7, result);
    }

    #endregion

    #region DeleteAllAsync Tests

    [Fact]
    public async Task DeleteAllAsync_RemovesAllTransactions()
    {
        // Arrange
        await SeedTransactions(5);

        // Act
        await _repository.DeleteAllAsync();

        // Assert
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DeleteAllAsync_EmptyDatabase_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _repository.DeleteAllAsync();

        var count = await _context.Transactions.CountAsync();
        Assert.Equal(0, count);
    }

    #endregion

    #region SaveChangesAsync Tests

    [Fact]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        // Arrange
        var transaction = CreateValidTransaction();
        await _context.Transactions.AddAsync(transaction);

        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result); // Should return number of affected rows
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(1, count);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid transaction for testing.
    /// </summary>
    private Transaction CreateValidTransaction(string? storeName = null)
    {
        return new Transaction
        {
            Type = TransactionType.Debit,
            Date = DateTime.Now.Date,
            Time = new TimeSpan(12, 0, 0),
            Amount = 100.00m,
            Cpf = "12345678901",
            CardNumber = "1234****5678",
            StoreOwner = "John Doe",
            StoreName = storeName ?? "Test Store",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Seeds database with test transactions.
    /// </summary>
    private async Task SeedTransactions(int count, string? storeName = null)
    {
        for (int i = 0; i < count; i++)
        {
            var transaction = CreateValidTransaction(storeName);
            await _context.Transactions.AddAsync(transaction);
        }
        await _context.SaveChangesAsync();
    }

    #endregion
}
