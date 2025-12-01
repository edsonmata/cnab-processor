// ========================================
// File: backend/src/CnabProcessor.Api/Controllers/CnabController.cs
// Purpose: REST API endpoints for CNAB file processing
// ========================================

using CnabProcessor.Api.Models;
using CnabProcessor.Api.Validators;
using CnabProcessor.Api.ViewModels;
using CnabProcessor.Domain.Interfaces;
using CnabProcessor.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace CnabProcessor.Api.Controllers
{
    /// <summary>
    /// Controller for CNAB file operations and transaction queries.
    /// JWT Authentication is ENABLED - all endpoints require authentication.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // ✅ JWT authentication ENABLED
    public class CnabController : ControllerBase
    {
        private readonly ICnabParser _parser;
        private readonly ITransactionRepository _repository;
        private readonly ILogger<CnabController> _logger;

        public CnabController(
            ICnabParser parser,
            ITransactionRepository repository,
            ILogger<CnabController> logger)
        {
            _parser = parser;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Uploads a CNAB file and saves the parsed transactions.
        /// </summary>
        /// <param name="file">CNAB file to upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload result with transaction count</returns>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(UploadResponseViewModel), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UploadCnab(
            [FromForm] IFormFile file,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate file with comprehensive checks
                var validationResult = CnabFileValidator.Validate(file);

                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("File validation failed: {Errors}", validationResult.GetErrorMessage());
                    return BadRequest(new UploadResponseViewModel
                    {
                        Success = false,
                        Message = $"File validation failed: {validationResult.GetErrorMessage()}"
                    });
                }

                // Log warnings if any
                if (validationResult.Warnings.Count > 0)
                {
                    _logger.LogWarning("File validation warnings: {Warnings}", validationResult.GetWarningMessage());
                }

                _logger.LogInformation("Processing CNAB file: {FileName} ({Size} bytes)",
                    file.FileName, file.Length);

                using var stream = file.OpenReadStream();

                // Parse transactions
                var transactions = await _parser.ParseAsync(stream, cancellationToken);
                var transactionList = transactions.ToList();

                if (transactionList.Count == 0)
                {
                    _logger.LogWarning("No valid transactions found in file: {FileName}", file.FileName);
                    return BadRequest(new UploadResponseViewModel
                    {
                        Success = false,
                        FileName = file.FileName,
                        Message = "No valid transactions found in the file."
                    });
                }

                // 🚀 OPTIMIZED: Use BulkInsertAsync for large files (>= 1000 records)
                // This is 10x faster than regular insert for big datasets!
                int insertedCount;

                if (transactionList.Count >= 1000)
                {
                    _logger.LogInformation(
                        "Large file detected ({Count} transactions). Using OPTIMIZED bulk insert...",
                        transactionList.Count);

                    // Use optimized bulk insert with batching
                    insertedCount = await _repository.BulkInsertAsync(
                        transactionList,
                        batchSize: 5000,
                        cancellationToken);
                }
                else
                {
                    // Use regular insert for small files
                    await _repository.AddRangeAsync(transactionList, cancellationToken);
                    insertedCount = await _repository.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("Successfully imported {Count} transactions from {FileName}",
                    insertedCount, file.FileName);

                return Ok(new UploadResponseViewModel
                {
                    Success = true,
                    TransactionCount = insertedCount,
                    FileName = file.FileName,
                    Message = $"Successfully imported {insertedCount} transactions!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CNAB file upload");
                return StatusCode(500, new UploadResponseViewModel
                {
                    Success = false,
                    Message = $"Error processing file: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Returns all transactions from a specific store.
        /// </summary>
        /// <param name="storeName">Name of the store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("store/{storeName}")]
        [ProducesResponseType(typeof(IEnumerable<TransactionViewModel>), 200)]
        public async Task<IActionResult> GetByStore(
            string storeName,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching transactions for store: {StoreName}", storeName);

            var transactions = await _repository.GetByStoreAsync(storeName, cancellationToken);

            var result = transactions.Select(t => new TransactionViewModel
            {
                Id = t.Id,
                Type = ((int)t.Type).ToString(),
                TypeDescription = t.TypeDescription,
                Nature = t.Nature.ToString(),
                Date = t.Date,
                Time = t.Time.ToString(@"hh\:mm\:ss"),
                Amount = t.Amount,
                SignedAmount = t.SignedAmount,
                Cpf = t.Cpf,
                CardNumber = t.CardNumber,
                StoreOwner = t.StoreOwner,
                StoreName = t.StoreName
            });

            return Ok(result);
        }

        /// <summary>
        /// Returns all transactions grouped by store with balance calculations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("balances")]
        [ProducesResponseType(typeof(IEnumerable<StoreBalanceViewModel>), 200)]
        public async Task<IActionResult> GetBalances(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching store balances");

            var balances = await _repository.GetStoreBalancesAsync(cancellationToken);

            var result = balances.Select(b => new StoreBalanceViewModel
            {
                StoreName = b.StoreName,
                TotalBalance = b.TotalBalance,
                TotalIncome = b.TotalIncome,
                TotalExpenses = b.TotalExpenses,
                TransactionCount = b.TransactionCount,
                Transactions = b.Transactions.Select(t => new TransactionViewModel
                {
                    Id = t.Id,
                    Type = ((int)t.Type).ToString(),
                    TypeDescription = t.TypeDescription,
                    Nature = t.Nature.ToString(),
                    Date = t.Date,
                    Time = t.Time.ToString(@"hh\:mm\:ss"),
                    Amount = t.Amount,
                    SignedAmount = t.SignedAmount,
                    Cpf = t.Cpf,
                    CardNumber = t.CardNumber,
                    StoreOwner = t.StoreOwner,
                    StoreName = t.StoreName
                }).ToList()
            });

            return Ok(result);
        }

        /// <summary>
        /// Returns general system statistics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(StatisticsViewModel), 200)]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching system statistics");

            var balances = await _repository.GetStoreBalancesAsync(cancellationToken);
            var balanceList = balances.ToList();

            var transactions = await _repository.GetAllAsync(cancellationToken);
            var transactionList = transactions.ToList();

            var stats = new StatisticsViewModel
            {
                TotalStores = balanceList.Count,
                TotalTransactions = transactionList.Count,
                TotalBalance = balanceList.Sum(x => x.TotalBalance),
                BiggestStore = balanceList
                    .OrderByDescending(x => x.TotalBalance)
                    .FirstOrDefault()?.StoreName,
                SmallestStore = balanceList
                    .OrderBy(x => x.TotalBalance)
                    .FirstOrDefault()?.StoreName
            };

            return Ok(stats);
        }

        /// <summary>
        /// Returns all transactions (deprecated - use /api/cnab/transactions/paged for better performance).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(IEnumerable<TransactionViewModel>), 200)]
        [Obsolete("Use /api/cnab/transactions/paged for paginated results")]
        public async Task<IActionResult> GetAllTransactions(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching all transactions");

            var transactions = await _repository.GetAllAsync(cancellationToken);

            var result = transactions.Select(t => new TransactionViewModel
            {
                Id = t.Id,
                Type = ((int)t.Type).ToString(),
                TypeDescription = t.TypeDescription,
                Nature = t.Nature.ToString(),
                Date = t.Date,
                Time = t.Time.ToString(@"hh\:mm\:ss"),
                Amount = t.Amount,
                SignedAmount = t.SignedAmount,
                Cpf = t.Cpf,
                CardNumber = t.CardNumber,
                StoreOwner = t.StoreOwner,
                StoreName = t.StoreName
            });

            return Ok(result);
        }

        /// <summary>
        /// Returns transactions with pagination (recommended for large datasets).
        /// </summary>
        /// <param name="pageNumber">Page number (1-based, default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("transactions/paged")]
        [ProducesResponseType(typeof(PagedResult<TransactionViewModel>), 200)]
        public async Task<IActionResult> GetTransactionsPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching transactions - Page {PageNumber}, Size {PageSize}", pageNumber, pageSize);

            var transactions = await _repository.GetAllAsync(cancellationToken);

            var viewModels = transactions.Select(t => new TransactionViewModel
            {
                Id = t.Id,
                Type = ((int)t.Type).ToString(),
                TypeDescription = t.TypeDescription,
                Nature = t.Nature.ToString(),
                Date = t.Date,
                Time = t.Time.ToString(@"hh\:mm\:ss"),
                Amount = t.Amount,
                SignedAmount = t.SignedAmount,
                Cpf = t.Cpf,
                CardNumber = t.CardNumber,
                StoreOwner = t.StoreOwner,
                StoreName = t.StoreName
            });

            var pagedResult = PagedResult<TransactionViewModel>.Create(viewModels, pageNumber, pageSize);

            return Ok(pagedResult);
        }

        /// <summary>
        /// Returns transactions for a specific store with pagination.
        /// </summary>
        /// <param name="storeName">Name of the store</param>
        /// <param name="pageNumber">Page number (1-based, default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("store/{storeName}/paged")]
        [ProducesResponseType(typeof(PagedResult<TransactionViewModel>), 200)]
        public async Task<IActionResult> GetByStorePaged(
            string storeName,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Fetching transactions for store: {StoreName} - Page {PageNumber}, Size {PageSize}",
                storeName, pageNumber, pageSize);

            var transactions = await _repository.GetByStoreAsync(storeName, cancellationToken);

            var viewModels = transactions.Select(t => new TransactionViewModel
            {
                Id = t.Id,
                Type = ((int)t.Type).ToString(),
                TypeDescription = t.TypeDescription,
                Nature = t.Nature.ToString(),
                Date = t.Date,
                Time = t.Time.ToString(@"hh\:mm\:ss"),
                Amount = t.Amount,
                SignedAmount = t.SignedAmount,
                Cpf = t.Cpf,
                CardNumber = t.CardNumber,
                StoreOwner = t.StoreOwner,
                StoreName = t.StoreName
            });

            var pagedResult = PagedResult<TransactionViewModel>.Create(viewModels, pageNumber, pageSize);

            return Ok(pagedResult);
        }


        [HttpDelete("transactions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteAllTransactions(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting all transactions from database");

                await _repository.DeleteAllAsync(cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = $"Successfully deleted all transactions",
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all transactions");
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error deleting transactions: {ex.Message}"
                });
            }
        }
    }
}