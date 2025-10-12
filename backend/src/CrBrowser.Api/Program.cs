using CrBrowser.Api;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization options globally (camelCase for enums)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddSingleton<IValidationService, ValidationService>();

// Bind registries configuration from appsettings.json
var registriesConfig = new RegistriesConfiguration();
builder.Configuration.GetSection("Registries").Bind(registriesConfig);
builder.Services.AddSingleton(registriesConfig);

// Registry factory for multi-registry support
builder.Services.AddHttpClient("GhcrClient", c =>
{
    c.BaseAddress = new Uri(registriesConfig.Ghcr.BaseUrl);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("DockerHubClient", c =>
{
    c.BaseAddress = new Uri(registriesConfig.DockerHub.BaseUrl);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("QuayClient", c =>
{
    c.BaseAddress = new Uri(registriesConfig.Quay.BaseUrl);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("GcrClient", c =>
{
    c.BaseAddress = new Uri(registriesConfig.Gcr.BaseUrl);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<IRegistryFactory, RegistryFactory>();

// GHCR client (HttpClient factory) - maintained for backward compatibility
builder.Services.AddHttpClient<IGhcrClient, GhcrClient>(c =>
{
    c.BaseAddress = new Uri(registriesConfig.Ghcr.BaseUrl);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("cr-browser/0.0.1");
    c.Timeout = TimeSpan.FromSeconds(30);
});
var startTime = DateTime.UtcNow;

// Core service registrations (minimal now; expanded in later tasks)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

// Placeholder health endpoint (now with uptimeSeconds to satisfy test expectation)
app.MapGet("/api/health", () => Results.Json(new { status = "ok", uptimeSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds }));

// Serve static OpenAPI file from specs path under /api/openapi.yaml (temporary approach)
var specPhysicalPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "specs", "001-ghcr-browser-is", "contracts", "openapi.yaml");
app.MapGet("/api/openapi.yaml", () =>
{
    if (!System.IO.File.Exists(specPhysicalPath))
    {
        return Results.NotFound();
    }
    return Results.File(specPhysicalPath, "application/yaml");
});

// Configure JSON serialization (camelCase)
app.Use(async (ctx, next) => { ctx.Response.Headers["X-App"] = "cr-browser"; await next(); });

// New multi-registry endpoint
app.MapGet("/api/registries/{registryType}/{owner}/{image}/tags", async (
    string registryType, 
    string owner, 
    string image, 
    int page = 1, 
    int pageSize = 10, 
    IValidationService validator = null!, 
    IRegistryFactory factory = null!, 
    CancellationToken ct = default) =>
{
    // Validate registry type
    if (!Enum.TryParse<RegistryType>(registryType, ignoreCase: true, out var type))
    {
        return Results.Json(new ErrorResponse("InvalidRegistryType", $"Invalid registry type: {registryType}", false), statusCode: 400);
    }

    // Validate owner/image format
    if (!validator.TryParseReference(owner, image, out var reference, out var error))
    {
        return Results.Json(new ErrorResponse("InvalidFormat", error ?? "Invalid reference", false), statusCode: 400);
    }

    // Validate page and pageSize
    if (page < 1)
    {
        return Results.Json(new ErrorResponse("InvalidPage", "Page must be >= 1", false), statusCode: 400);
    }
    if (pageSize < 1 || pageSize > 100)
    {
        return Results.Json(new ErrorResponse("InvalidPageSize", "PageSize must be between 1 and 100", false), statusCode: 400);
    }

    // Create appropriate client
    var client = factory.CreateClient(type);

    // Fetch all tags (same logic as legacy endpoint for now)
    var all = new List<string>();
    string? last = null;
    bool hasMore = true;
    int safety = 200;
    int attempts = 0;
    while (hasMore && attempts < safety)
    {
        var (tags, notFound, retryable, upstreamHasMore) = await client.ListTagsPageAsync(owner, image, 100, last, ct);
        if (notFound)
        {
            return Results.Json(new ErrorResponse("NotFound", "Repository not found", false), statusCode: 404);
        }
        if (retryable)
        {
            return Results.Json(new ErrorResponse("TransientUpstream", "Upstream temporary error. Please retry.", true), statusCode: 503);
        }
        if (tags.Count == 0 && attempts == 0)
        {
            return Results.Json(new ErrorResponse("NotFound", "Repository not found", false), statusCode: 404);
        }
        foreach (var t in tags)
        {
            if (t.StartsWith("sha256", StringComparison.OrdinalIgnoreCase)) continue;
            all.Add(t);
        }
        hasMore = upstreamHasMore;
        last = tags.Count > 0 ? tags[^1] : last;
        attempts++;
    }

    return Results.Json(new { tags = all });
});

// Legacy endpoint: returns all tag names (excluding digest-like) with no pagination/enrichment
app.MapGet("/api/images/{owner}/{image}/tags", async (string owner, string image, IValidationService validator, IRegistryFactory factory, CancellationToken ct) =>
{
    if (!validator.TryParseReference(owner, image, out var reference, out var error))
    {
        return Results.Json(new ErrorResponse("InvalidFormat", error ?? "Invalid reference", false), statusCode: 400);
    }

    var client = factory.CreateClient(RegistryType.Ghcr);

    var all = new List<string>();
    string? last = null;
    bool hasMore = true;
    int safety = 200; // allow up to 200 requests (worst case many skipped digest tags)
    int attempts = 0;
    while (hasMore && attempts < safety)
    {
        var (tags, notFound, retryable, upstreamHasMore) = await client.ListTagsPageAsync(owner, image, 100, last, ct);
        if (notFound)
        {
            return Results.Json(new ErrorResponse("NotFound", "Repository not found", false), statusCode: 404);
        }
        if (retryable)
        {
            return Results.Json(new ErrorResponse("TransientUpstream", "Upstream temporary error. Please retry.", true), statusCode: 503);
        }
        if (tags.Count == 0 && attempts == 0)
        {
            return Results.Json(new ErrorResponse("NotFound", "Repository not found", false), statusCode: 404);
        }
        foreach (var t in tags)
        {
            if (t.StartsWith("sha256", StringComparison.OrdinalIgnoreCase)) continue;
            all.Add(t);
        }
        hasMore = upstreamHasMore;
        last = tags.Count > 0 ? tags[^1] : last;
        attempts++;
    }

    // Return a very simple shape for frontend consumption
    return Results.Json(new { tags = all });
});

app.Run();
