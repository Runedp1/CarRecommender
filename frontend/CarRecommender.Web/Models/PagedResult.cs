namespace CarRecommender.Web.Models;

/// <summary>
/// Helper class voor paginatie resultaten.
/// Komt overeen met de backend PagedResult class.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

