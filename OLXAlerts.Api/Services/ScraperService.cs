using System.Diagnostics;
using System.Text.RegularExpressions;
using OLXAlerts.Api.Entities;

namespace OLXAlerts.Api.Services;

public partial class ScraperService(IConfiguration config, ILogger<ScraperService> logger)
{
    private readonly string _scriptPath = config["Scraper:ScriptPath"] ?? "/app/scraper/scraper.py";
    private readonly string _pythonBin = config["Scraper:PythonBin"] ?? "python3";

    [GeneratedRegex(@"SCRAPER_RESULT:inserted=(\d+),total=(\d+)")]
    private static partial Regex ResultPattern();

    public async Task<(int Inserted, int Total)> RunAsync(SearchJob job, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _pythonBin,
            ArgumentList = { _scriptPath, "--job-id", job.Id.ToString(), "--search-term", job.SearchTerm, "--location", job.LocationCode },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (job.CategoryId.HasValue)
        {
            psi.ArgumentList.Add("--category-id");
            psi.ArgumentList.Add(job.CategoryId.Value.ToString());
        }

        if (job.MinPrice.HasValue)
        {
            psi.ArgumentList.Add("--min-price");
            psi.ArgumentList.Add(job.MinPrice.Value.ToString("F0"));
        }

        if (job.MaxPrice.HasValue)
        {
            psi.ArgumentList.Add("--max-price");
            psi.ArgumentList.Add(job.MaxPrice.Value.ToString("F0"));
        }

        // Pass DB credentials via environment variables (not CLI args) to keep secrets out of process list
        var conn = config.GetConnectionString("Postgres") ?? string.Empty;
        var parsed = ParseConnectionString(conn);
        psi.Environment["PGHOST"] = parsed.GetValueOrDefault("host", "localhost");
        psi.Environment["PGPORT"] = parsed.GetValueOrDefault("port", "5432");
        psi.Environment["PGDATABASE"] = parsed.GetValueOrDefault("database", "olxalerts");
        psi.Environment["PGUSER"] = parsed.GetValueOrDefault("username", "postgres");
        psi.Environment["PGPASSWORD"] = parsed.GetValueOrDefault("password", "");

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (!string.IsNullOrEmpty(stderr))
            logger.LogWarning("Scraper stderr for job {JobId}: {Stderr}", job.Id, stderr);

        logger.LogInformation("Scraper stdout for job {JobId}:\n{Stdout}", job.Id, stdout);

        var match = ResultPattern().Match(stdout);
        if (!match.Success)
        {
            logger.LogWarning("No SCRAPER_RESULT line found in scraper output for job {JobId}", job.Id);
            return (0, 0);
        }

        return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
    }

    /// <summary>Parses a Npgsql-style connection string into a dictionary.</summary>
    private static Dictionary<string, string> ParseConnectionString(string connectionString)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            var key = part[..idx].Trim().ToLowerInvariant();
            var value = part[(idx + 1)..].Trim();
            // Normalize common Npgsql keys to Postgres env var equivalents
            var mapped = key switch
            {
                "host" or "server" => "host",
                "port" => "port",
                "database" or "initial catalog" => "database",
                "username" or "user id" or "uid" => "username",
                "password" or "pwd" => "password",
                _ => key,
            };
            dict[mapped] = value;
        }
        return dict;
    }
}
