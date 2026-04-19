using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PROHUB.API.Server.Dtos;

namespace PROHUB.API.Server.Services;

/// <summary>
/// Aggregates tech news/repos from Hacker News, dev.to and GitHub Search.
/// Results are cached per tag-set for 20 minutes to respect rate limits.
/// </summary>
public class TrendsService(HttpClient http, IMemoryCache cache, ILogger<TrendsService> logger)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<TrendItem>> GetTrendsAsync(string[] tags, CancellationToken ct = default)
    {
        var cacheKey = $"trends:{string.Join(",", tags.Order())}";
        if (cache.TryGetValue(cacheKey, out List<TrendItem>? cached) && cached is not null)
            return cached;

        var query = string.Join(" ", tags);

        var tasks = new[]
        {
            FetchHnAsync(query, ct),
            FetchDevToAsync(tags, ct),
            FetchGithubAsync(query, ct),
        };

        var results = await Task.WhenAll(tasks);

        var merged = results
            .SelectMany(r => r)
            .OrderByDescending(r => r.Points)
            .DistinctBy(r => r.Url)
            .Take(40)
            .ToList();

        cache.Set(cacheKey, merged, TimeSpan.FromMinutes(20));
        return merged;
    }

    // ── Hacker News via Algolia ─────────────────────────────────────────────────

    private async Task<List<TrendItem>> FetchHnAsync(string query, CancellationToken ct)
    {
        try
        {
            var url = $"https://hn.algolia.com/api/v1/search_by_date?tags=story&query={Uri.EscapeDataString(query)}&hitsPerPage=15&numericFilters=created_at_i%3E{DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds()}";
            var res = await http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return [];

            using var stream = await res.Content.ReadAsStreamAsync(ct);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var hits = doc.RootElement.GetProperty("hits");
            var items = new List<TrendItem>();
            foreach (var hit in hits.EnumerateArray())
            {
                var title = hit.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var urlProp = hit.TryGetProperty("url", out var u) ? u.GetString() : null;
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(urlProp)) continue;

                var points = hit.TryGetProperty("points", out var p) ? (p.ValueKind == JsonValueKind.Number ? p.GetInt32() : 0) : 0;
                var author = hit.TryGetProperty("author", out var a) ? a.GetString() : null;
                var createdAt = hit.TryGetProperty("created_at", out var ca) ? ca.GetString() ?? "" : "";
                var objectId = hit.TryGetProperty("objectID", out var oid) ? oid.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

                items.Add(new TrendItem(
                    objectId, "hn", title, urlProp, null, author, points, createdAt, []));
            }
            return items;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Trends] HN fetch failed");
            return [];
        }
    }

    // ── dev.to ──────────────────────────────────────────────────────────────────

    private async Task<List<TrendItem>> FetchDevToAsync(string[] tags, CancellationToken ct)
    {
        var items = new List<TrendItem>();
        foreach (var tag in tags.Take(3))
        {
            try
            {
                var url = $"https://dev.to/api/articles?tag={Uri.EscapeDataString(tag)}&per_page=8&top=7";
                http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PROHUB-Manager/1.0");
                var res = await http.GetAsync(url, ct);
                if (!res.IsSuccessStatusCode) continue;

                using var stream = await res.Content.ReadAsStreamAsync(ct);
                var articles = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                foreach (var article in articles.RootElement.EnumerateArray())
                {
                    var title = article.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    var articleUrl = article.TryGetProperty("url", out var u) ? u.GetString() : null;
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(articleUrl)) continue;

                    var desc = article.TryGetProperty("description", out var d) ? d.GetString() : null;
                    var author = article.TryGetProperty("user", out var userProp) && userProp.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var reactions = article.TryGetProperty("public_reactions_count", out var r) ? (r.ValueKind == JsonValueKind.Number ? r.GetInt32() : 0) : 0;
                    var published = article.TryGetProperty("published_at", out var pa) ? pa.GetString() ?? "" : "";
                    var id = article.TryGetProperty("id", out var idProp) ? idProp.GetInt32().ToString() : Guid.NewGuid().ToString();

                    var articleTags = article.TryGetProperty("tag_list", out var tagList)
                        ? tagList.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x != "").ToArray()
                        : [];

                    items.Add(new TrendItem(
                        $"devto-{id}", "devto", title, articleUrl, desc, author, reactions, published, articleTags));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Trends] dev.to fetch failed for tag {Tag}", tag);
            }
        }
        return items;
    }

    // ── GitHub Search ───────────────────────────────────────────────────────────

    private async Task<List<TrendItem>> FetchGithubAsync(string query, CancellationToken ct)
    {
        try
        {
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PROHUB-Manager/1.0");
            var url = $"https://api.github.com/search/repositories?q={Uri.EscapeDataString(query)}&sort=stars&order=desc&per_page=10";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "PROHUB-Manager/1.0");
            req.Headers.Add("Accept", "application/vnd.github.v3+json");

            var res = await http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return [];

            using var stream = await res.Content.ReadAsStreamAsync(ct);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (!doc.RootElement.TryGetProperty("items", out var reposElem)) return [];

            var items = new List<TrendItem>();
            foreach (var repo in reposElem.EnumerateArray())
            {
                var name = repo.TryGetProperty("full_name", out var fn) ? fn.GetString() ?? "" : "";
                var repoUrl = repo.TryGetProperty("html_url", out var u) ? u.GetString() : null;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(repoUrl)) continue;

                var desc = repo.TryGetProperty("description", out var d) ? d.GetString() : null;
                var stars = repo.TryGetProperty("stargazers_count", out var s) ? (s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0) : 0;
                var owner = repo.TryGetProperty("owner", out var ownerProp) && ownerProp.TryGetProperty("login", out var l) ? l.GetString() : null;
                var updated = repo.TryGetProperty("updated_at", out var ua) ? ua.GetString() ?? "" : "";
                var lang = repo.TryGetProperty("language", out var lang2) && lang2.ValueKind == JsonValueKind.String ? lang2.GetString() : null;
                var id = repo.TryGetProperty("id", out var idProp) ? idProp.GetInt64().ToString() : Guid.NewGuid().ToString();

                items.Add(new TrendItem(
                    $"gh-{id}", "github", name, repoUrl, desc, owner, stars, updated,
                    lang != null ? [lang] : []));
            }
            return items;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Trends] GitHub fetch failed");
            return [];
        }
    }
}
