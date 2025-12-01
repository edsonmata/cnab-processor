// ========================================
// File: CnabProcessor.IntegrationTests/CnabApiIntegrationTests.cs
// Purpose: Integration tests for CNAB API endpoints
// ========================================

using CnabProcessor.Api.ViewModels;
using CnabProcessor.Domain.Enums;
using CnabProcessor.Domain.Extensions;
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
/// Integration tests for CNAB API endpoints.
/// Uses WebApplicationFactory to spin up in-memory test server.
/// </summary>
public class CnabApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public CnabApiIntegrationTests(WebApplicationFactory<Program> factory)
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

    public void Dispose()
    {
        _client?.Dispose();
    }

    #region Health Check Tests

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content);
    }

    #endregion

    #region Upload Tests

    [Fact]
    public async Task Upload_ValidCnabFile_ReturnsSuccess()
    {
        // Arrange
        var cnabContent = CreateValidCnabContent();
        var content = CreateFileContent(cnabContent, "CNAB.txt");

        // Act
        var response = await _client.PostAsync("/api/cnab/upload", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UploadResponseViewModel>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(3, result.TransactionCount);
        Assert.Contains("Successfully imported", result.Message);
        Assert.Equal("CNAB.txt", result.FileName);

        // Verify database

        using (var context = GetDbContext())
        {
            var transactions = await context.Transactions.ToListAsync();
            Assert.Equal(3, transactions.Count);
        }
    }

    [Fact]
    public async Task Upload_NoFile_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync("/api/cnab/upload", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UploadResponseViewModel>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("No file was uploaded", result.Message);
    }

    [Fact]
    public async Task Upload_InvalidCnabFormat_ReturnsBadRequest()
    {
        // Arrange
        var invalidContent = "This is not a valid CNAB file\nInvalid format";
        var content = CreateFileContent(invalidContent, "invalid.txt");

        // Act
        var response = await _client.PostAsync("/api/cnab/upload", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UploadResponseViewModel>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        // Updated to match new validator behavior (validates earlier, better!)
        Assert.Contains("File validation failed", result.Message);
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var content = CreateFileContent("", "empty.txt");

        // Act
        var response = await _client.PostAsync("/api/cnab/upload", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_MultipleFiles_ProcessesCorrectly()
    {
        // Arrange - First upload
        var cnabContent1 = CreateValidCnabContent();
        var content1 = CreateFileContent(cnabContent1, "CNAB1.txt");

        // Act - First upload
        var response1 = await _client.PostAsync("/api/cnab/upload", content1);

        // Assert - First upload
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var result1 = await response1.Content.ReadFromJsonAsync<UploadResponseViewModel>();
        Assert.Equal(3, result1.TransactionCount);

        // Arrange - Second upload
        var cnabContent2 = CreateValidCnabContent(startDate: "20190302");
        var content2 = CreateFileContent(cnabContent2, "CNAB2.txt");

        // Act - Second upload
        var response2 = await _client.PostAsync("/api/cnab/upload", content2);

        // Assert - Second upload
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var result2 = await response2.Content.ReadFromJsonAsync<UploadResponseViewModel>();
        Assert.Equal(3, result2.TransactionCount);

        // Verify total in database
        using (var context = GetDbContext())
        {
            var transactions = await context.Transactions.ToListAsync();
            Assert.Equal(6, transactions.Count);
        }
    }

    #endregion

    #region Get Transactions Tests

    [Fact]
    public async Task GetAllTransactions_WithData_ReturnsTransactions()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/cnab/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>();
        Assert.NotNull(transactions);
        Assert.Equal(3, transactions.Count);

        // Verify transaction
        var transaction = transactions.Where(it => it.StoreName.Equals("BAR DO JOÃO")).First();
        Assert.Equal(TransactionType.Financing.GetDescription(), transaction.TypeDescription);
        Assert.Equal(TransactionNature.Expense.GetDescription(), transaction.Nature);
    }

    [Fact]
    public async Task GetAllTransactions_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/cnab/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>();
        Assert.NotNull(transactions);
        Assert.Empty(transactions);
    }

    #endregion

    #region Get By Store Tests

    [Fact]
    public async Task GetByStore_ExistingStore_ReturnsTransactions()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/cnab/store/BAR%20DO%20JO%C3%83O");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>();
        Assert.NotNull(transactions);
        Assert.All(transactions, t => Assert.Equal("BAR DO JOÃO", t.StoreName));
    }

    [Fact]
    public async Task GetByStore_NonExistingStore_ReturnsEmptyList()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/cnab/store/LOJA_INEXISTENTE");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>();
        Assert.NotNull(transactions);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task GetByStore_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/cnab/store/MERCEARIA%203%20IRM%C3%83OS");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>();
        Assert.NotNull(transactions);
        Assert.All(transactions, t => Assert.Equal("MERCEARIA 3 IRMÃOS", t.StoreName));
    }

    #endregion

    #region Get Balances Tests

    [Fact]
    public async Task GetBalances_WithData_ReturnsBalances()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/cnab/balances");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balances = await response.Content.ReadFromJsonAsync<List<StoreBalanceViewModel>>();
        Assert.NotNull(balances);
        Assert.NotEmpty(balances);

        // Verify balance calculation
        var barDoJoao = balances.FirstOrDefault(b => b.StoreName == "BAR DO JOÃO");
        Assert.NotNull(barDoJoao);
        Assert.True(barDoJoao.TotalBalance < 0);
        Assert.True(barDoJoao.TransactionCount > 0);
        Assert.NotEmpty(barDoJoao.Transactions);
    }

    [Fact]
    public async Task GetBalances_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/cnab/balances");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balances = await response.Content.ReadFromJsonAsync<List<StoreBalanceViewModel>>();
        Assert.NotNull(balances);
        Assert.Empty(balances);
    }

    [Fact]
    public async Task GetBalances_MixedTransactions_CalculatesCorrectly()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/cnab/balances");

        // Assert
        var balances = await response.Content.ReadFromJsonAsync<List<StoreBalanceViewModel>>();
        Assert.NotNull(balances);

        // Verify calculations for each store
        foreach (var store in balances)
        {
            Assert.Equal(
                store.TotalIncome - store.TotalExpenses,
                store.TotalBalance
            );
        }
    }

    #endregion

    #region Get Stats Tests

    [Fact]
    public async Task GetStats_WithData_ReturnsCorrectStats()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/cnab/stats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<StatisticsViewModel>();
        Assert.NotNull(stats);
        Assert.Equal(3, stats.TotalTransactions);
        Assert.True(stats.TotalStores > 0);
        Assert.NotNull(stats.BiggestStore);
        Assert.NotNull(stats.SmallestStore);
    }

    [Fact]
    public async Task GetStats_EmptyDatabase_ReturnsZeroStats()
    {
        // Act
        var response = await _client.GetAsync("/api/cnab/stats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<StatisticsViewModel>();
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalTransactions);
        Assert.Equal(0, stats.TotalStores);
        Assert.Equal(0, stats.TotalBalance);
    }

    #endregion

    #region Delete All Transactions

    [Fact]
    public async Task DeleteAllTransactions_WhenDatabaseHasData_ShouldDeleteEverything()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/cnab/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionViewModel>>();
        Assert.NotNull(transactions);
        Assert.Equal(3, transactions.Count);

        // Act
        var deleteResponse = await _client.DeleteAsync("/api/cnab/transactions");

        // Assert
        deleteResponse.EnsureSuccessStatusCode();
        var message = await deleteResponse.Content.ReadAsStringAsync();
        Assert.Contains("successfully", message, StringComparison.OrdinalIgnoreCase);

        var afterDelete = await _client.GetAsync("/api/cnab/transactions");
        var afterJson = await afterDelete.Content.ReadFromJsonAsync<List<TransactionViewModel>>();
        Assert.NotNull(afterJson);
        Assert.Empty(afterJson!);
    }

    [Fact]
    public async Task DeleteAllTransactions_WhenDatabaseIsEmpty_ShouldStillReturnSuccess()
    {
        // Act
        var deleteResponse = await _client.DeleteAsync("/api/cnab/transactions");

        // Assert
        deleteResponse.EnsureSuccessStatusCode();

        var msg = await deleteResponse.Content.ReadAsStringAsync();
        Assert.Contains("success", msg, StringComparison.OrdinalIgnoreCase);

        var getResponse = await _client.GetAsync("/api/cnab/transactions");
        var transactions = await getResponse.Content.ReadFromJsonAsync<List<TransactionViewModel>>();

        Assert.NotNull(transactions);
        Assert.Empty(transactions!);
    }

    #endregion

    #region End-to-End Workflow Tests

    [Fact]
    public async Task EndToEndWorkflow_UploadAndQuery_WorksCorrectly()
    {
        // Step 1: Upload file
        var cnabContent = CreateValidCnabContent();
        var uploadContent = CreateFileContent(cnabContent, "CNAB.txt");
        var uploadResponse = await _client.PostAsync("/api/cnab/upload", uploadContent);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        using (var context = GetDbContext())
        {
            var savedCount = await context.Transactions.CountAsync();
            Assert.True(savedCount > 0, $"Expected transactions to be saved, but found {savedCount}");
        }

        // Step 3: Get all transactions via API
        var transactionsResponse = await _client.GetAsync("/api/cnab/transactions");
        Assert.Equal(HttpStatusCode.OK, transactionsResponse.StatusCode);

        var transactions2 = await transactionsResponse.Content
            .ReadFromJsonAsync<List<TransactionViewModel>>();

        Assert.NotNull(transactions2);
        Assert.NotEmpty(transactions2);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates valid CNAB file content for testing.
    /// </summary>
    private string CreateValidCnabContent(string startDate = "20190301")
    {
        return string.Join("\n", new[]
        {
            $"3{startDate}0000014200096206760174753****3153141358JOÃO MACEDO   BAR DO JOÃO       ",
            $"5{startDate}0000013200556418150633648****0099153453MARIA SILVA   MERCEARIA 3 IRMÃOS",
            $"2{startDate}0000012200845152540736777****1313172712MARCOS PEREIRA LOJA DO Ó - MATRIZ"
        });
    }

    /// <summary>
    /// Creates multipart form data content for file upload.
    /// </summary>
    private MultipartFormDataContent CreateFileContent(string content, string fileName)
    {
        var multipartContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        multipartContent.Add(fileContent, "file", fileName);
        return multipartContent;
    }

    /// <summary>
    /// Seeds database with test data.
    /// </summary>
    private async Task SeedDatabase()
    {
        var cnabContent = CreateValidCnabContent();
        var content = CreateFileContent(cnabContent, "seed.txt");
        var response = await _client.PostAsync("/api/cnab/upload", content);
        response.EnsureSuccessStatusCode();
    }

    #endregion
}