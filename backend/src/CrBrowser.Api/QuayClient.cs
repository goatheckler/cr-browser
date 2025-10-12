using System.Text.Json;

namespace CrBrowser.Api;

public sealed class QuayClient : OciRegistryClientBase
{
    public override RegistryType RegistryType => RegistryType.Quay;
    public override string BaseUrl => "https://quay.io";

    public QuayClient(HttpClient http, ILogger<QuayClient> logger) : base(http, logger)
    {
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://quay.io/");
    }

    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        var authUrl = $"https://quay.io/v2/auth?service=quay.io&scope=repository:{repository}:pull";
        
        using var authClient = new HttpClient();
        authClient.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
        
        using var req = new HttpRequestMessage(HttpMethod.Get, authUrl);
        var resp = await authClient.SendAsync(req, ct);
        
        if (!resp.IsSuccessStatusCode) return null;
        
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("token", out var t))
                return t.GetString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Quay authentication token for repository {Repository}", repository);
        }
        
        return null;
    }

    public override string FormatFullReference(string owner, string image, string tag)
    {
        var repo = FormatRepositoryPath(owner, image);
        return $"quay.io/{repo}:{tag}";
    }
}
