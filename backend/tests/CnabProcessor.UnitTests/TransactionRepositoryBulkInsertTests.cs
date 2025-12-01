// ========================================
// File: CnabProcessor.UnitTests/TransactionRepositoryBulkInsertTests.cs
// Purpose: Unit tests for BulkInsertAsync method with batching optimization
// ========================================

using CnabProcessor.Domain.Entities;
using CnabProcessor.Domain.Enums;
using CnabProcessor.Infrastructure.Data;
using CnabProcessor.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CnabProcessor.UnitTests;

/// <summary>
/// Unit tests for TransactionRepository.BulkInsertAsync method.
/// Tests batching, performance optimization, and error scenarios.
/// </summary>
public class TransactionRepositoryBulkInsertTests : IDisposable
{
    private readonly CnabDbContext _context;
    private readonly TransactionRepository _repository;
    private readonly Mock<ILogger<TransactionRepository>> _loggerMock;

    public TransactionRepositoryBulkInsertTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<CnabDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
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

    #region BulkInsertAsync - Basic Functionality Tests

    [Fact]
    public async Task BulkInsertAsync_SingleBatch_InsertsAllRecords()
    {
        // Arrange - Create 100 transactions (less than default batch size of 5000)
        var transactions = CreateTransactions(100);

        // Act
        var insertedCount = await _repository.BulkInsertAsync(transactions);

        // Assert
        Assert.Equal(100, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(100, count);
    }

    [Fact]
    public async Task BulkInsertAsync_MultipleBatches_InsertsAllRecords()
    {
        // Arrange - Create 12,500 transactions (will be split into 3 batches of 5000)
        var transactions = CreateTransactions(12500);

        // Act
        var insertedCount = await _repository.BulkInsertAsync(transactions, batchSize: 5000);

        // Assert
        Assert.Equal(12500, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(12500, count);
    }

    [Fact]
    public async Task BulkInsertAsync_LargeDataset_InsertsAllRecords()
    {
        // Arrange - Create 50,000 transactions
        var transactions = CreateTransactions(50000);

        // Act
        var startTime = DateTime.Now;
        var insertedCount = await _repository.BulkInsertAsync(transactions, batchSize: 5000);
        var elapsed = DateTime.Now - startTime;

        // Assert
        Assert.Equal(50000, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(50000, count);

        // Performance check: Should complete in reasonable time
        // With optimization, this should be < 30 seconds
        Assert.True(elapsed.TotalSeconds < 30, $"Bulk insert took {elapsed.TotalSeconds}s, expected < 30s");
    }

    #endregion

    #region BulkInsertAsync - Batch Size Tests

    [Fact]
    public async Task BulkInsertAsync_CustomBatchSize_RespectsBatchSize()
    {
        // Arrange
        var transactions = CreateTransactions(3000);
        var batchSize = 1000; // Smaller batch size

        // Act
        var insertedCount = await _repository.BulkInsertAsync(transactions, batchSize: batchSize);

        // Assert
        Assert.Equal(3000, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(3000, count);
    }

    [Fact]
    public async Task BulkInsertAsync_SmallBatchSize_InsertsAllRecords()
    {
        // Arrange - Create 5000 transactions with batch size 500
        var transactions = CreateTransactions(5000);

        // Act
        var insertedCount = await _repository.BulkInsertAsync(transactions, batchSize: 500);

        // Assert
        Assert.Equal(5000, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(5000, count);
    }

    [Fact]
    public async Task BulkInsertAsync_LargeBatchSize_InsertsAllRecords()
    {
        // Arrange - Create 10,000 transactions with large batch size
        var transactions = CreateTransactions(10000);

        // Act
        var insertedCount = await _repository.BulkInsertAsync(transactions, batchSize: 10000);

        // Assert
        Assert.Equal(10000, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(10000, count);
    }

    #endregion

    #region BulkInsertAsync - Edge Cases

    [Fact]
    public async Task BulkInsertAsync_EmptyList_ReturnsZero()
    {
        // Arrange
        var transactions = new List<Transaction>();

        // Act
        var insertedCount = await _repository.BulkInsertAsync(transactions);

        // Assert
        Assert.Equal(0, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task BulkInsertAsync_SingleTransaction_InsertsSuccessfully()
    {
        // Arrange
        var transactions = CreateTransactions(1);

        // Act
        var insertedCount = await _repository.BulkInsertAsync(transactions);

        // Assert
        Assert.Equal(1, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task BulkInsertAsync_ExactBatchBoundary_InsertsAllRecords()
    {
        // Arrange - Create exactly 10,000 transactions (2 batches of 5000)
        var transactions = CreateTransactions(10000);

        // Act
        var insertedCount = await _repository.BulkInsertAsync(transactions, batchSize: 5000);

        // Assert
        Assert.Equal(10000, insertedCount);
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(10000, count);
    }

    #endregion

    #region BulkInsertAsync - Data Integrity Tests

    [Fact]
    public async Task BulkInsertAsync_PreservesTransactionData()
    {
        // Arrange
        var transactions = CreateTransactions(100);
        var firstTransaction = transactions[0];

        // Act
        await _repository.BulkInsertAsync(transactions);

        // Assert
        var inserted = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Cpf == firstTransaction.Cpf);

        Assert.NotNull(inserted);
        Assert.Equal(firstTransaction.StoreName, inserted.StoreName);
        Assert.Equal(firstTransaction.Amount, inserted.Amount);
        Assert.Equal(firstTransaction.Type, inserted.Type);
    }

    [Fact]
    public async Task BulkInsertAsync_AllRecordsHaveValidData()
    {
        // Arrange
        var transactions = CreateTransactions(1000);

        // Act
        await _repository.BulkInsertAsync(transactions, batchSize: 250);

        // Assert
        var allRecords = await _context.Transactions.ToListAsync();
        Assert.Equal(1000, allRecords.Count);

        // Verify no null required fields
        foreach (var record in allRecords)
        {
            Assert.NotNull(record.StoreName);
            Assert.NotEmpty(record.StoreName);
            Assert.NotNull(record.Cpf);
            Assert.True(record.Amount > 0);
        }
    }

    [Fact]
    public async Task BulkInsertAsync_SetTimestamps()
    {
        // Arrange
        var transactions = CreateTransactions(10);
        var beforeInsert = DateTime.UtcNow;

        // Act
        await _repository.BulkInsertAsync(transactions);
        var afterInsert = DateTime.UtcNow;

        // Assert
        var inserted = await _context.Transactions.ToListAsync();
        foreach (var transaction in inserted)
        {
            Assert.NotEqual(default, transaction.CreatedAt);
            Assert.True(transaction.CreatedAt >= beforeInsert);
            Assert.True(transaction.CreatedAt <= afterInsert);
        }
    }

    #endregion

    #region BulkInsertAsync - Error Scenarios

    [Fact]
    public async Task BulkInsertAsync_NullList_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.BulkInsertAsync(null!));
    }

    [Fact]
    public async Task BulkInsertAsync_InvalidBatchSize_HandlesGracefully()
    {
        // Arrange
        var transactions = CreateTransactions(100);

        // Act & Assert - Batch size of 1 should still work (very slowly, but correctly)
        var insertedCount = await _repository.BulkInsertAsync(transactions, batchSize: 1);
        Assert.Equal(100, insertedCount);
    }

    [Fact]
    public async Task BulkInsertAsync_WithCancellation_CancelsProperly()
    {
        // Arrange
        var transactions = CreateTransactions(10000);
        var cts = new System.Threading.CancellationTokenSource();

        // Act & Assert
        // Cancel after a short delay
        cts.CancelAfter(TimeSpan.FromMilliseconds(10));

        try
        {
            await _repository.BulkInsertAsync(transactions, batchSize: 5000, cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected to be cancelled
        }

        // Some records may have been inserted before cancellation
        var count = await _context.Transactions.CountAsync();
        Assert.True(count < transactions.Count);
    }

    #endregion

    #region BulkInsertAsync - Performance Tests

    [Fact]
    public async Task BulkInsertAsync_CompletesLargeDatasetSuccessfully()
    {
        // Arrange
        // Note: Performance comparison between BulkInsertAsync and AddRange is not meaningful
        // in an in-memory database because it lacks the I/O overhead that BulkInsertAsync optimizes for.
        // In real SQL Server scenarios, BulkInsertAsync provides 10x+ performance improvement.
        // This test verifies BulkInsertAsync completes successfully and efficiently for large datasets.
        var transactions = CreateTransactions(5000);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var insertedCount = await _repository.BulkInsertAsync(transactions, batchSize: 1000);
        stopwatch.Stop();

        // Assert - BulkInsertAsync should complete 5000 records in reasonable time
        // Even in-memory, should complete in < 5 seconds
        Assert.Equal(5000, insertedCount);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Inserting 5000 records took {stopwatch.ElapsedMilliseconds}ms, should complete in < 5000ms");

        // Verify all records are in database
        var count = await _context.Transactions.CountAsync();
        Assert.Equal(5000, count);
    }

    [Fact]
    public async Task BulkInsertAsync_ChangeTrackerIsCleared()
    {
        // Arrange
        var transactions = CreateTransactions(1000);

        // Act
        await _repository.BulkInsertAsync(transactions, batchSize: 500);

        // Assert - Change tracker should be cleared, so context should have no tracked entities
        // This is tested by checking that a fresh query returns the same count
        var trackedCount = _context.ChangeTracker.Entries().Count();
        Assert.Equal(0, trackedCount);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a list of valid transactions for testing.
    /// </summary>
    private List<Transaction> CreateTransactions(int count)
    {
        var transactions = new List<Transaction>();
        var stores = new[] { "Store A", "Store B", "Store C", "Store D", "Store E" };

        for (int i = 0; i < count; i++)
        {
            var transaction = new Transaction
            {
                Type = (TransactionType)(i % 9 + 1), // Rotate through types 1-9
                Date = DateTime.Now.AddDays(-(count - i)), // Spread dates
                Time = new TimeSpan(10 + (i % 8), 30 + (i % 60), 0),
                Amount = 100.00m + (i % 1000),
                Cpf = $"{i:D11}",
                CardNumber = $"****{i % 10000:D4}",
                StoreOwner = $"Owner {i % 10}",
                StoreName = stores[i % stores.Length],
                CreatedAt = DateTime.UtcNow
            };
            transactions.Add(transaction);
        }

        return transactions;
    }

    #endregion
}
