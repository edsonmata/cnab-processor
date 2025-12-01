// ========================================
// File: CnabProcessor.IntegrationTests/PagedEndpointsIntegrationTests.cs
// Purpose: Integration tests for paginated API endpoints
// ========================================

using CnabProcessor.Api.Models;
using CnabProcessor.Api.ViewModels;
using CnabProcessor.Domain.Entities;
using CnabProcessor.Domain.Enums;
using CnabProcessor.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using Xunit;

namespace CnabProcessor.IntegrationTests;

/// <summary>
/// Integration tests for paginated endpoints.
/// Tests /api/cnab/transactions/paged and /api/cnab/store/{storeName}/paged
/// </summary>
public class PagedEndpointsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public PagedEndpointsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _databaseName = $"TestDatabase_{Guid.NewGuid()}";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove DbContext
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<CnabDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<CnabDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                services.Configure<ApiBehaviorOptions>(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });

                // Add fake authentication for testing
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>("Test", options => { });
            });
        });

        _client = _factory.CreateClient();
    }

    private CnabDbContext GetDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CnabDbContext>();
    }

    private async Task SeedTransactions(int count, string? storeName = null)
    {
        using (var context = GetDbContext())
        {
            for (int i = 0; i < count; i++)
            {
                var transaction = new Transaction
                {
                    Type = (TransactionType)((i % 9) + 1),
                    Date = DateTime.Now.AddDays(-(count - i)),
                    Time = new TimeSpan(10, 0, 0),
                    Amount = 100.00m + i,
                    Cpf = $"{i:D11}",
                    CardNumber = $"****{i % 10000:D4}",
                    StoreOwner = $"Owner {i % 5}",
                    StoreName = storeName ?? $"Store {i % 3}",
                    CreatedAt = DateTime.UtcNow
                };
                await context.Transactions.AddAsync(transaction);
            }
            await context.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    #region GET /api/cnab/transactions/paged Tests

    [Fact]
    public async Task GetTransactionsPaged_DefaultPagination_ReturnsFirstPage()
    {
        // Arrange
        await SeedTransactions(100);

        // Act
        var response = await _client.GetAsync("/api/cnab/transactions/paged?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(10, result.TotalPages);
        Assert.Equal(10, result.Items.Count());
        Assert.True(result.HasNext);
        Assert.False(result.HasPrevious);
    }

    [Fact]
    public async Task GetTransactionsPaged_MiddlePage_ReturnsCorrectData()
    {
        // Arrange
        await SeedTransactions(100);

        // Act
        var response = await _client.GetAsync("/api/cnab/transactions/paged?pageNumber=5&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(10, result.Items.Count());
        Assert.True(result.HasNext);
        Assert.True(result.HasPrevious);
    }

    [Fact]
    public async Task GetTransactionsPaged_LastPage_ReturnsCorrectData()
    {
        // Arrange
        await SeedTransactions(100);

        // Act
        var response = await _client.GetAsync("/api/cnab/transactions/paged?pageNumber=10&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(10, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(10, result.Items.Count());
        Assert.False(result.HasNext);
        Assert.True(result.HasPrevious);
    }

    [Fact]
    public async Task GetTransactionsPaged_CustomPageSize_ReturnsCorrectCount()
    {
        // Arrange
        await SeedTransactions(100);

        // Act
        var response = await _client.GetAsync("/api/cnab/transactions/paged?pageNumber=1&pageSize=25");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(25, result.Items.Count());
    }

    [Fact]
    public async Task GetTransactionsPaged_PageOutOfRange_RedirectsToLastPage()
    {
        // Arrange
        await SeedTransactions(50);

        // Act
        var response = await _client.GetAsync("/api/cnab/transactions/paged?pageNumber=100&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        // Should redirect to last valid page
        Assert.Equal(5, result.PageNumber);
        Assert.Equal(10, result.Items.Count());
    }

    [Fact]
    public async Task GetTransactionsPaged_InvalidPageNumber_HandlesBadRequest()
    {
        // Arrange
        await SeedTransactions(50);

        // Act - Page number 0 or negative
        var response = await _client.GetAsync("/api/cnab/transactions/paged?pageNumber=0&pageSize=10");

        // Assert - Should redirect to page 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactionsPaged_EmptyDatabase_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/api/cnab/transactions/paged?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetTransactionsPaged_SinglePageResult_ReturnsAllRecords()
    {
        // Arrange - Create 15 transactions, request 20 per page
        await SeedTransactions(15);

        // Act
        var response = await _client.GetAsync("/api/cnab/transactions/paged?pageNumber=1&pageSize=20");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(15, result.Items.Count());
        Assert.False(result.HasNext);
        Assert.False(result.HasPrevious);
    }

    #endregion

    #region GET /api/cnab/store/{storeName}/paged Tests

    [Fact]
    public async Task GetStoreTransactionsPaged_ExistingStore_ReturnsPagedTransactions()
    {
        // Arrange
        await SeedTransactions(50, "BAR DO JOÃO");

        // Act
        var response = await _client.GetAsync("/api/cnab/store/BAR%20DO%20JO%C3%83O/paged?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(5, result.TotalPages);
        Assert.Equal(10, result.Items.Count());
    }

    [Fact]
    public async Task GetStoreTransactionsPaged_NonExistentStore_ReturnsEmptyResults()
    {
        // Arrange
        await SeedTransactions(50, "Store A");

        // Act
        var response = await _client.GetAsync("/api/cnab/store/NonExistent/paged?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetStoreTransactionsPaged_MultiplePages_VerifyPagination()
    {
        // Arrange
        await SeedTransactions(75, "MERCEARIA 3 IRMÃOS");

        // Act - Get page 2
        var response = await _client.GetAsync("/api/cnab/store/MERCEARIA%203%20IRM%C3%83OS/paged?pageNumber=2&pageSize=25");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(75, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(25, result.Items.Count());
        Assert.True(result.HasNext);
        Assert.True(result.HasPrevious);
    }

    [Fact]
    public async Task GetStoreTransactionsPaged_SpecialCharactersInName_ReturnsCorrectResults()
    {
        // Arrange
        var storeName = "CAFÉ DO JOSÉ";
        await SeedTransactions(30, storeName);

        // Act
        var response = await _client.GetAsync($"/api/cnab/store/{Uri.EscapeDataString(storeName)}/paged?pageNumber=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(30, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetStoreTransactionsPaged_LastPagePartialResults_ReturnsCorrectly()
    {
        // Arrange
        await SeedTransactions(77, "Test Store");

        // Act - Get last page (should have 7 items)
        var response = await _client.GetAsync("/api/cnab/store/Test%20Store/paged?pageNumber=8&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.Equal(8, result.PageNumber);
        Assert.Equal(7, result.Items.Count());
        Assert.False(result.HasNext);
        Assert.True(result.HasPrevious);
    }

    #endregion

    #region Error Scenarios Tests

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(0, 10)]
    public async Task GetTransactionsPaged_InvalidPageNumber_HandlesGracefully(int pageNumber, int pageSize)
    {
        // Arrange
        await SeedTransactions(50);

        // Act
        var response = await _client.GetAsync($"/api/cnab/transactions/paged?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert - Should still return success, redirecting to valid page
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.True(result.PageNumber > 0);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 1000)] // Exceeds reasonable max
    public async Task GetTransactionsPaged_InvalidPageSize_HandlesGracefully(int pageNumber, int pageSize)
    {
        // Arrange
        await SeedTransactions(50);

        // Act
        var response = await _client.GetAsync($"/api/cnab/transactions/paged?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        // Should either clamp values or return error with valid data
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
        Assert.NotNull(result);
        Assert.True(result.PageSize > 0);
    }

    #endregion

    #region Integration with Data Consistency Tests

    [Fact]
    public async Task GetTransactionsPaged_VerifyDataConsistency()
    {
        // Arrange
        await SeedTransactions(100);

        // Act - Get all pages sequentially
        var allItems = new List<TransactionViewModel>();
        for (int page = 1; page <= 10; page++)
        {
            var response = await _client.GetAsync($"/api/cnab/transactions/paged?pageNumber={page}&pageSize=10");
            var result = await response.Content.ReadFromJsonAsync<PagedResult<TransactionViewModel>>();
            allItems.AddRange(result!.Items);
        }

        // Assert - Should have all 100 unique items
        Assert.Equal(100, allItems.Count);
        Assert.Equal(100, allItems.DistinctBy(x => x.Id).Count()); // All unique IDs
    }

    #endregion
}
