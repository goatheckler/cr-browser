using System.Net.Http.Headers;
using System.Text.Json;

namespace GhcrBrowser.Api;



public interface IGhcrClient
{
    Task<(IReadOnlyList<string> Tags, bool NotFound, bool Retryable, bool HasMore)> ListTagsPageAsync(string owner, string image, int pageSize, string? last, CancellationToken ct = default);
}

public sealed class GhcrClient : IGhcrClient
{

    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public GhcrClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://ghcr.io/");
    }

    private sealed record TagListResponse(List<string> Tags, string? Name = null); // minimal fields we care about
    private sealed record ErrorEntry(string Code, string? Message);
    private sealed record ErrorEnvelope(List<ErrorEntry> Errors);


    public async Task<(IReadOnlyList<string> Tags, bool NotFound, bool Retryable, bool HasMore)> ListTagsPageAsync(string owner, string image, int pageSize, string? last, CancellationToken ct = default)
    {
        // Normalize repository path: ghcr uses owner/image
        var repository = $"{owner}/{image}".ToLowerInvariant();
        var url = $"v2/{repository}/tags/list?n={pageSize}" + (string.IsNullOrEmpty(last) ? string.Empty : $"&last={Uri.EscapeDataString(last)}");

        // First attempt without token
        var (resp, retry, notFound) = await SendAsync(url, bearer: null, ct);
        if (notFound) return (Array.Empty<string>(), true, false, false);
        if (resp is null && retry)
        {
            // Need token
            var token = await AcquireTokenAsync(repository, ct);
            (resp, retry, notFound) = await SendAsync(url, token, ct);
            if (notFound) return (Array.Empty<string>(), true, false, false);
            if (resp is null && token == null && retry)
            {
                // Could not acquire token (private or non-existent) treat as not found for public browsing UX
                return (Array.Empty<string>(), true, false, false);
            }
        }

        if (resp is null)
        {
            // Unhandled transient or auth failure
            return (Array.Empty<string>(), false, retry, false);
        }

        try
        {
            await using var s = await resp.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<TagListResponse>(s, JsonOpts, ct);
            var tags = data?.Tags ?? new List<string>();
            bool hasMore = false;

            // GHCR may emit Link header for pagination. Format typically: <https://ghcr.io/v2/OWNER/IMAGE/tags/list?n=100&last=TAG>; rel="next"
            if (resp.Headers.TryGetValues("Link", out var linkVals))
            {
                foreach (var link in linkVals)
                {
                    // naive detection of rel="next"
                    if (link.Contains("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
                    {
                        hasMore = true;
                        break;
                    }
                }
            }
            else
            {
                // Fallback heuristic: if we exactly filled the page, assume maybe more (cannot know for sure)
                if (tags.Count == pageSize) hasMore = true;
            }

            return (tags, false, false, hasMore);
        }
        catch
        {
            return (Array.Empty<string>(), false, true, false); // treat parse errors as retryable
        }
    }

    private async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        var tokenUrl = $"token?scope=repository:{repository}:pull&service=ghcr.io";
        using var req = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("token", out var t))
                return t.GetString();
        }
        catch { }
        return null;
    }

    private async Task<(HttpResponseMessage? Response, bool Retryable, bool NotFound)> SendAsync(string relativeUrl, string? bearer, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        if (!string.IsNullOrWhiteSpace(bearer))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        var resp = await _http.SendAsync(req, ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, false, true);
        }
        if ((int)resp.StatusCode == 429 || (int)resp.StatusCode >= 500)
        {
            return (null, true, false);
        }
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Need token
            return (null, true, false);
        }
        if (!resp.IsSuccessStatusCode)
        {
            return (null, false, false);
        }
        return (resp, false, false);
    }


}
