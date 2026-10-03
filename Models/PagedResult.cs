using Microsoft.EntityFrameworkCore;

namespace cateringflow.Models;

/// <summary>
/// Non-generic surface shared by all paged results so partials/views can
/// render pagination without depending on the item type.
/// </summary>
public interface IPagedResult
{
    int Page { get; }
    int PageSize { get; }
    int TotalItems { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
    int FirstItem { get; }
    int LastItem { get; }
    IEnumerable<int> GetPageWindow();
}

/// <summary>
/// Carries a single page of rows plus pagination metadata.
/// Page size defaults to 10 per the admin-panel requirements.
/// </summary>
public class PagedResult<T> : IPagedResult
{
    public const int DefaultPageSize = 10;
    public const int PageLinkWindow = 5;

    public required IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize)));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItem => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItem => Math.Min(Page * PageSize, TotalItems);

    /// <summary>
    /// Page numbers to render, bounded around the current page with ellipsis gaps.
    /// </summary>
    public IEnumerable<int> GetPageWindow()
    {
        var total = TotalPages;
        if (total <= PageLinkWindow)
        {
            return Enumerable.Range(1, total);
        }

        var start = Math.Max(1, Page - PageLinkWindow / 2);
        var end = Math.Min(total, start + PageLinkWindow - 1);
        start = Math.Max(1, end - PageLinkWindow + 1);
        return Enumerable.Range(start, end - start + 1);
    }

    public static async Task<PagedResult<T>> CreateAsync(
        IQueryable<T> source,
        int? page,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var total = await source.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)Math.Max(1, pageSize)));
        var current = Math.Clamp(page ?? 1, 1, totalPages);

        var items = await source
            .Skip((current - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            Page = current,
            PageSize = pageSize,
            TotalItems = total
        };
    }
}