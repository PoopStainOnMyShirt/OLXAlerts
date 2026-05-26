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
        var telegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();

        var now = DateTime.UtcNow;

        var dueJobs = await db.SearchJobs
            .Where(j => j.IsActive && j.NextRunAt <= now)
            .ToListAsync(ct);

        // Stagger job runs to spread OLX API load — avoids burst requests
        // when many jobs fire at the same scheduler tick.
        for (int i = 0; i < dueJobs.Count; i++)
        {
            await RunJobAsync(db, scraper, whatsApp, telegram, dueJobs[i], ct);

            // 15–30 s gap between consecutive jobs (skip after the last one).
            if (i < dueJobs.Count - 1)
            {
                var interJobDelay = TimeSpan.FromSeconds(Random.Shared.Next(15, 31));
                logger.LogInformation("Waiting {Delay}s before next job to avoid rate limiting...", interJobDelay.TotalSeconds);
                await Task.Delay(interJobDelay, ct);
            }
        }
    }

    public async Task RunJobAsync(
        AppDbContext db,
        ScraperService scraper,
        IWhatsAppService whatsApp,
        ITelegramService telegram,
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
                string? sid;
                AlertLog log;

                if (job.NotificationChannel == NotificationChannel.Telegram && job.TelegramChatId.HasValue)
                {
                    sid = await telegram.SendAlertAsync(job.TelegramChatId.Value, listing);
                    log = new AlertLog
                    {
                        JobId = job.Id,
                        ListingId = listing.Id,
                        TelegramChatId = job.TelegramChatId,
                        MessageSid = sid,
                        Status = sid is null ? "failed" : "sent",
                    };
                }
                else if (job.NotificationChannel == NotificationChannel.WhatsApp && job.WhatsAppNumber is not null)
                {
                    sid = await whatsApp.SendAlertAsync(job.WhatsAppNumber, listing);
                    log = new AlertLog
                    {
                        JobId = job.Id,
                        ListingId = listing.Id,
                        WhatsAppNumber = job.WhatsAppNumber,
                        MessageSid = sid,
                        Status = sid is null ? "failed" : "sent",
                    };
                }
                else
                {
                    logger.LogWarning("Job {JobId} has no valid notification channel configured, skipping listing {ListingId}", job.Id, listing.Id);
                    continue;
                }

                db.AlertLogs.Add(log);

                // 1–2 s gap between notifications — Telegram allows 1 msg/s per chat.
                await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(1000, 2001)), ct);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running job {JobId}", job.Id);
        }
    }
}
