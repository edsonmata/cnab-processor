// ========================================
// File: CnabProcessor.Api/Models/PagedResult.cs
// Purpose: Generic paged result model
// ========================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CnabProcessor.Api.Models;

/// <summary>
/// Represents a paged result with metadata for pagination.
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Indicates if there is a previous page.
    /// </summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Indicates if there is a next page.
    /// </summary>
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// Creates a paged result from a query.
    /// </summary>
    /// <param name="source">The source collection</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paged result</returns>
    public static PagedResult<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        // Ensure valid page number
        if (pageNumber < 1) pageNumber = 1;

        // Ensure valid page size (between 1 and 100)
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var sourceList = source.ToList();
        var totalCount = sourceList.Count;

        // If page number exceeds total pages, return last page
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        if (pageNumber > totalPages && totalPages > 0)
        {
            pageNumber = totalPages;
        }

        var items = sourceList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}

/// <summary>
/// Parameters for pagination requests.
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    /// <summary>
    /// Page number (1-based). Default is 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Items per page. Default is 10, maximum is 100.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1) ? 10 : value;
    }
}
