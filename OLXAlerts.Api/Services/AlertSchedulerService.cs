using Microsoft.EntityFrameworkCore;
using OLXAlerts.Api.Data;
using OLXAlerts.Api.Entities;

namespace OLXAlerts.Api.Services;

public class AlertSchedulerService(
    IServiceScopeFactory scopeFactory,
    ILogger<AlertSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AlertSchedulerService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessDueJobsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        logger.LogInformation("AlertSchedulerService stopped.");
    }

    private async Task ProcessDueJobsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var scraper = scope.ServiceProvider.GetRequiredService<ScraperService>();
        var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        var now = DateTime.UtcNow;

        var dueJobs = await db.SearchJobs
            .Where(j => j.IsActive && j.NextRunAt <= now)
            .ToListAsync(ct);

        foreach (var job in dueJobs)
        {
            await RunJobAsync(db, scraper, whatsApp, job, ct);
        }
    }

    public async Task RunJobAsync(
        AppDbContext db,
        ScraperService scraper,
        IWhatsAppService whatsApp,
        SearchJob job,
        CancellationToken ct)
    {
        var cutoff = job.LastRunAt ?? DateTime.UtcNow.AddHours(-1);
        var now = DateTime.UtcNow;

        // Update next_run_at before running — prevents double-fire if run takes longer than interval
        job.NextRunAt = now.AddMinutes(job.IntervalMinutes);
        job.LastRunAt = now;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Running scraper for job {JobId} ({SearchTerm})...", job.Id, job.SearchTerm);

        try
        {
            var (inserted, total) = await scraper.RunAsync(job, ct);
            logger.LogInformation("Job {JobId}: inserted={Inserted}, total={Total}", job.Id, inserted, total);

            if (inserted == 0) return;

            // Find listings scraped in this run window
            var newListings = await db.Listings
                .Where(l => l.JobId == job.Id && l.ScrapedAt >= cutoff)
                .ToListAsync(ct);

            foreach (var listing in newListings)
            {
                var sid = await whatsApp.SendAlertAsync(job.WhatsAppNumber, listing);
                var log = new AlertLog
                {
                    JobId = job.Id,
                    ListingId = listing.Id,
                    WhatsAppNumber = job.WhatsAppNumber,
                    MessageSid = sid,
                    Status = sid is null ? "failed" : "sent",
                };
                db.AlertLogs.Add(log);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running job {JobId}", job.Id);
        }
    }
}
