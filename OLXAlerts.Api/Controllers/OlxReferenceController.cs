using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace OLXAlerts.Api.Controllers;

/// <summary>
/// Proxies OLX reference data (locations, categories) using Playwright so
/// OLX's anti-bot protection is bypassed. Responses are cached in-process
/// for 1 hour.
/// </summary>
[ApiController]
[Route("api/olx")]
[EnableRateLimiting("reference")]
public class OlxReferenceController(IConfiguration config, ILogger<OlxReferenceController> logger) : ControllerBase
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
        var data = await GetCachedAsync(_locationsCache, "https://www.olx.in/api/locations/", "locations", HttpContext.RequestAborted);
        return data is null ? StatusCode(502, "Failed to fetch OLX locations") : Ok(data);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var data = await GetCachedAsync(_categoriesCache, "https://www.olx.in/api/categories/", "categories", HttpContext.RequestAborted);
        return data is null ? StatusCode(502, "Failed to fetch OLX categories") : Ok(data);
    }

    private async Task<JsonElement?> GetCachedAsync(CacheEntry cache, string url, string name, CancellationToken ct)
    {
        if (cache.FetchedAt != default && DateTime.UtcNow - cache.FetchedAt < CacheTtl)
            return cache.Data;

        await _lock.WaitAsync(ct);
        try
        {
            if (cache.FetchedAt != default && DateTime.UtcNow - cache.FetchedAt < CacheTtl)
                return cache.Data;

            logger.LogInformation("Fetching OLX {Name} via Playwright…", name);
            var json = await FetchViaPlaywrightAsync(url, ct);
            var doc = JsonDocument.Parse(json);
            cache.Data = doc.RootElement.Clone();
            cache.FetchedAt = DateTime.UtcNow;
            logger.LogInformation("Fetched OLX {Name} ({Bytes} bytes)", name, json.Length);
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

    /// <summary>
    /// Spawns fetch_reference.py with Playwright to retrieve the URL, bypassing
    /// OLX's anti-bot detection that blocks plain HttpClient requests.
    /// </summary>
    private async Task<string> FetchViaPlaywrightAsync(string url, CancellationToken ct)
    {
        var scriptPath = config["Scraper:ScriptPath"] ?? "/app/scraper/scraper.py";
        var scriptDir = Path.GetDirectoryName(scriptPath) ?? ".";
        var fetchScript = Path.Combine(scriptDir, "fetch_reference.py");
        var pythonBin = config["Scraper:PythonBin"] ?? "python";

        var psi = new ProcessStartInfo
        {
            FileName = pythonBin,
            ArgumentList = { fetchScript, "--url", url },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (!string.IsNullOrWhiteSpace(stderr))
            logger.LogWarning("fetch_reference.py stderr: {Stderr}", stderr);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"fetch_reference.py exited {process.ExitCode}: {stderr}");

        if (string.IsNullOrWhiteSpace(stdout))
            throw new InvalidOperationException("fetch_reference.py returned empty output");

        return stdout.Trim();
    }
}
