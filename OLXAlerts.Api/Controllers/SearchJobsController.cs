using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OLXAlerts.Api.Data;
using OLXAlerts.Api.DTOs;
using OLXAlerts.Api.Entities;
using OLXAlerts.Api.Services;

namespace OLXAlerts.Api.Controllers;

[ApiController]
[Route("api/search-jobs")]
[EnableRateLimiting("api")]
public class SearchJobsController(
    AppDbContext db,
    ScraperService scraperService,
    IWhatsAppService whatsApp,
    AlertSchedulerService scheduler,
    ILogger<SearchJobsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SearchJobResponseDto>>> GetAll()
    {
        var jobs = await db.SearchJobs
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => ToDto(j))
            .ToListAsync();
        return Ok(jobs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SearchJobResponseDto>> GetById(int id)
    {
        var job = await db.SearchJobs.FindAsync(id);
        if (job is null) return NotFound();
        return Ok(ToDto(job));
    }

    [HttpPost]
    public async Task<ActionResult<SearchJobResponseDto>> Create([FromBody] CreateSearchJobDto dto)
    {
        var job = new SearchJob
        {
            SearchTerm = dto.SearchTerm.Trim(),
            LocationCode = dto.LocationCode,
            LocationName = dto.LocationName,
            CategoryId = dto.CategoryId,
            WhatsAppNumber = dto.WhatsAppNumber,
            IntervalMinutes = dto.IntervalMinutes,
        };
        db.SearchJobs.Add(job);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ToDto(job));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SearchJobResponseDto>> Update(int id, [FromBody] UpdateSearchJobDto dto)
    {
        var job = await db.SearchJobs.FindAsync(id);
        if (job is null) return NotFound();

        if (dto.SearchTerm is not null) job.SearchTerm = dto.SearchTerm.Trim();
        if (dto.LocationCode is not null) job.LocationCode = dto.LocationCode;
        if (dto.LocationName is not null) job.LocationName = dto.LocationName;
        if (dto.CategoryId.HasValue) job.CategoryId = dto.CategoryId;
        if (dto.IntervalMinutes.HasValue) job.IntervalMinutes = dto.IntervalMinutes.Value;
        if (dto.IsActive.HasValue) job.IsActive = dto.IsActive.Value;

        await db.SaveChangesAsync();
        return Ok(ToDto(job));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await db.SearchJobs.FindAsync(id);
        if (job is null) return NotFound();
        db.SearchJobs.Remove(job);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/run")]
    [EnableRateLimiting("scraper-run")]
    public async Task<IActionResult> RunNow(int id)
    {
        var job = await db.SearchJobs.FindAsync(id);
        if (job is null) return NotFound();

        logger.LogInformation("Manual trigger for job {JobId}", id);
        await scheduler.RunJobAsync(db, scraperService, whatsApp, job, HttpContext.RequestAborted);

        return Ok(new { message = $"Scraper run completed for job {id}." });
    }

    [HttpGet("{id:int}/listings")]
    public async Task<IActionResult> GetListings(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await db.SearchJobs.AnyAsync(j => j.Id == id))
            return NotFound();

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = db.Listings.Where(l => l.JobId == id).OrderByDescending(l => l.ScrapedAt);
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.PriceDisplay,
                l.Location,
                l.UserName,
                l.IsBusiness,
                l.AdId,
                l.ScrapedAt,
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    private static SearchJobResponseDto ToDto(SearchJob j) => new()
    {
        Id = j.Id,
        SearchTerm = j.SearchTerm,
        LocationCode = j.LocationCode,
        LocationName = j.LocationName,
        CategoryId = j.CategoryId,
        WhatsAppNumber = j.WhatsAppNumber,
        IntervalMinutes = j.IntervalMinutes,
        IsActive = j.IsActive,
        CreatedAt = j.CreatedAt,
        LastRunAt = j.LastRunAt,
        NextRunAt = j.NextRunAt,
    };
}
