using System.Text.Json;

namespace CrBrowser.Api;

public interface IGhcrClient
{
    Task<(IReadOnlyList<string> Tags, bool NotFound, bool Retryable, bool HasMore)> ListTagsPageAsync(string owner, string image, int pageSize, string? last, CancellationToken ct = default);
}

public sealed class GhcrClient : OciRegistryClientBase, IGhcrClient
{
    public override RegistryType RegistryType => RegistryType.Ghcr;
    public override string BaseUrl => "https://ghcr.io";

    public GhcrClient(HttpClient http, ILogger<GhcrClient> logger) : base(http, logger)
    {
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://ghcr.io/");
    }

    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        var tokenUrl = $"token?scope=repository:{repository}:pull&service=ghcr.io";
        using var req = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Token request failed with status {StatusCode} for {Repository}", resp.StatusCode, repository);
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("token", out var t))
                return t.GetString();
            
            _logger.LogWarning("Token response missing 'token' property for {Repository}", repository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse token response for {Repository}", repository);
        }
        return null;
    }

    public override string FormatFullReference(string owner, string image, string tag)
    {
        var repo = FormatRepositoryPath(owner, image);
        return $"ghcr.io/{repo}:{tag}";
    }

    async Task<(IReadOnlyList<string> Tags, bool NotFound, bool Retryable, bool HasMore)> IGhcrClient.ListTagsPageAsync(
        string owner, 
        string image, 
        int pageSize, 
        string? last, 
        CancellationToken ct)
    {
        var response = await base.ListTagsPageAsync(owner, image, pageSize, last, ct);
        return (response.Tags, response.NotFound, response.Retryable, response.HasMore);
    }
}

