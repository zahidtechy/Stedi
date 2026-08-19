namespace Stedi.Healthcare.Pagination;

/// <summary>
/// A single page of Stedi list results.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class StediPage<T>
{
    /// <summary>Initializes a page.</summary>
    public StediPage(IReadOnlyList<T> items, string? nextPageToken, int? totalCount = null)
    {
        Items = items;
        NextPageToken = nextPageToken;
        TotalCount = totalCount;
    }

    /// <summary>Items in this page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Token for the next page. Pass this value as <c>pageToken</c> on the subsequent request.
    /// Null or empty means this is the last page.
    /// </summary>
    public string? NextPageToken { get; }

    /// <summary>Total match count when the API returns one.</summary>
    public int? TotalCount { get; }

    /// <summary>Whether another page is available.</summary>
    public bool HasMore => !string.IsNullOrEmpty(NextPageToken);
}
