namespace Wajub;

/// <summary>Holds a page of list results.</summary>
public class PagedResult
{
    public List<Dictionary<string, object?>> Data { get; init; } = [];

    public Dictionary<string, object?>? Meta { get; init; }

    public bool HasMore { get; init; }

    internal Func<CancellationToken, int, Task<PagedResult>>? FetchPage { get; init; }

    public Task<PagedResult> GetNextPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasMore)
        {
            throw new InvalidOperationException("no more pages available");
        }

        if (FetchPage is null)
        {
            throw new InvalidOperationException("pagination handler is not configured");
        }

        var current = 1.0;
        if (Meta is not null
            && JsonUtils.TryGetDouble(Meta.GetValueOrDefault("current_page"), out var page))
        {
            current = page;
        }

        return FetchPage(cancellationToken, (int)current + 1);
    }

    public async Task<List<Dictionary<string, object?>>> AutoPagingIterAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<Dictionary<string, object?>>();
        var page = this;

        while (true)
        {
            all.AddRange(page.Data);
            if (!page.HasMore)
            {
                break;
            }

            page = await page.GetNextPageAsync(cancellationToken);
        }

        return all;
    }
}

internal static class Pagination
{
    public static Task<PagedResult> CreatePagedListAsync(
        BaseClient client,
        string path,
        string pluralKey,
        Dictionary<string, object?>? parameters,
        CancellationToken cancellationToken = default)
    {
        async Task<PagedResult> FetchPageAsync(CancellationToken ct, int pageNum)
        {
            var query = new Dictionary<string, object?>();
            if (parameters is not null)
            {
                foreach (var (key, value) in parameters)
                {
                    query[key] = value;
                }
            }

            query["page"] = pageNum;

            var response = await client.GetAsync(path, query, ct);
            var (data, meta) = JsonUtils.PickList(response, pluralKey);

            var hasMore = false;
            if (meta is not null
                && JsonUtils.TryGetDouble(meta.GetValueOrDefault("current_page"), out var current)
                && JsonUtils.TryGetDouble(meta.GetValueOrDefault("last_page"), out var last))
            {
                hasMore = current < last;
            }

            return new PagedResult
            {
                Data = data,
                Meta = meta,
                HasMore = hasMore,
                FetchPage = FetchPageAsync,
            };
        }

        var page = 1;
        if (parameters is not null && parameters.TryGetValue("page", out var pageValue))
        {
            if (pageValue is int pageInt)
            {
                page = pageInt;
            }
            else if (JsonUtils.TryGetDouble(pageValue, out var pageDouble))
            {
                page = (int)pageDouble;
            }
        }

        return FetchPageAsync(cancellationToken, page);
    }
}
