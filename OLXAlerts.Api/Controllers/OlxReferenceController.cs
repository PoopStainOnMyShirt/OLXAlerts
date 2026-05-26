using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace OLXAlerts.Api.Controllers;

/// <summary>
/// Proxies OLX reference data (locations, categories) so clients can browse
/// valid IDs without hitting the OLX API directly.
/// Responses are cached in-process for 1 hour.
/// </summary>
[ApiController]
[Route("api/olx")]
[EnableRateLimiting("reference")]
public class OlxReferenceController(IHttpClientFactory httpFactory, ILogger<OlxReferenceController> logger) : ControllerBase
{
    private sealed class CacheEntry
    {
        public DateTime FetchedAt { get; set; }
        public JsonElement Data { get; set; }
    }

    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly CacheEntry _locationsCache = new();
    private static readonly CacheEntry _categoriesCache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    [HttpGet("locations")]
    public async Task<IActionResult> GetLocations()
    {
        var data = await GetCachedAsync(_locationsCache, "https://www.olx.in/api/locations/", "locations");
        return data is null ? StatusCode(502, "Failed to fetch OLX locations") : Ok(data);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var data = await GetCachedAsync(_categoriesCache, "https://www.olx.in/api/categories/", "categories");
        return data is null ? StatusCode(502, "Failed to fetch OLX categories") : Ok(data);
    }

    private async Task<JsonElement?> GetCachedAsync(CacheEntry cache, string url, string name)
    {
        if (cache.FetchedAt != default && DateTime.UtcNow - cache.FetchedAt < CacheTtl)
            return cache.Data;

        await _lock.WaitAsync();
        try
        {
            if (cache.FetchedAt != default && DateTime.UtcNow - cache.FetchedAt < CacheTtl)
                return cache.Data;

            var client = httpFactory.CreateClient("olx");
            var json = await client.GetStringAsync(url);
            var doc = JsonDocument.Parse(json);
            cache.Data = doc.RootElement.Clone();
            cache.FetchedAt = DateTime.UtcNow;
            logger.LogInformation("Fetched OLX {Name} reference data", name);
            return cache.Data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch OLX {Name}", name);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }
}
