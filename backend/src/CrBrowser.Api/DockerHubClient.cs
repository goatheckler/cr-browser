using System.Text.Json;

namespace CrBrowser.Api;

public sealed class DockerHubClient : OciRegistryClientBase
{
    public override RegistryType RegistryType => RegistryType.DockerHub;
    public override string BaseUrl => "https://registry-1.docker.io";

    public DockerHubClient(HttpClient http) : base(http)
    {
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://registry-1.docker.io/");
    }

    protected override async Task<string?> AcquireTokenAsync(string repository, CancellationToken ct)
    {
        var authUrl = $"https://auth.docker.io/token?service=registry.docker.io&scope=repository:{repository}:pull";
        
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
        catch { }
        
        return null;
    }

    protected override string FormatRepositoryPath(string owner, string image)
    {
        if (string.IsNullOrEmpty(owner) || owner.Equals("library", StringComparison.OrdinalIgnoreCase))
        {
            return $"library/{image}".ToLowerInvariant();
        }
        
        return $"{owner}/{image}".ToLowerInvariant();
    }

    public override string FormatFullReference(string owner, string image, string tag)
    {
        var repo = FormatRepositoryPath(owner, image);
        return $"docker.io/{repo}:{tag}";
    }
}
