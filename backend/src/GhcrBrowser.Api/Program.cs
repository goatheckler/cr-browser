using GhcrBrowser.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IValidationService, ValidationService>();
// Removed unused truncation service
// Removed unused metadata state service
// Removed unused retry policy
// Removed unused in-flight request registry
// GHCR client (HttpClient factory)
builder.Services.AddHttpClient<IGhcrClient, GhcrClient>(c =>
{
    c.BaseAddress = new Uri("https://ghcr.io/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("ghcr-browser/0.0.1");
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
app.Use(async (ctx, next) => { ctx.Response.Headers["X-App"] = "ghcr-browser"; await next(); });

// Simplified tags endpoint: returns all tag names (excluding digest-like) with no pagination/enrichment
app.MapGet("/api/images/{owner}/{image}/tags", async (string owner, string image, IValidationService validator, IGhcrClient ghcr, CancellationToken ct) =>
{
    if (!validator.TryParseReference(owner, image, out var reference, out var error))
    {
        return Results.Json(new ErrorResponse("InvalidFormat", error ?? "Invalid reference", false), statusCode: 400);
    }

    var all = new List<string>();
    string? last = null;
    bool hasMore = true;
    int safety = 200; // allow up to 200 requests (worst case many skipped digest tags)
    int attempts = 0;
    while (hasMore && attempts < safety)
    {
        var (tags, notFound, retryable, upstreamHasMore) = await ghcr.ListTagsPageAsync(owner, image, 100, last, ct);
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
