# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Commands

### .NET API
```bash
cd OLXAlerts.Api
dotnet build
dotnet run                          # Swagger at https://localhost:5001/swagger
dotnet ef migrations add <Name>     # Generate new EF migration
dotnet ef database update           # Apply migrations to local DB
```

### Python scraper (run from `scraper/`)
```bash
cd scraper
PGHOST=localhost PGPORT=5432 PGDATABASE=olxalerts PGUSER=postgres PGPASSWORD=<pass> \
  python scraper.py --job-id 1 --search-term "Honda City" --location 1000001 --category-id 84
```

### Database
Local PostgreSQL is on port 5432 (native install, not Docker). Docker Compose maps to 5433.
```bash
# Apply migrations locally
cd OLXAlerts.Api && dotnet ef database update
```

## Architecture

Two separate processes share one PostgreSQL database:

- **Python scraper** writes listings rows directly via psycopg2
- **.NET API** owns the schema (EF Core migrations), orchestrates scraper as a subprocess, and sends WhatsApp alerts

### Data flow
1. `AlertSchedulerService` (BackgroundService, polls every 30s) finds jobs where `next_run_at <= NOW()`
2. Updates `next_run_at = now + interval_minutes` **before** running (prevents double-fire)
3. Calls `ScraperService.RunAsync()` which spawns `python scraper.py` as a subprocess
4. DB credentials are passed as env vars (`PGHOST` etc.), never as CLI args
5. Scraper prints `SCRAPER_RESULT:inserted=N,total=M` — .NET parses this line from stdout
6. New listings (scraped_at >= cutoff) trigger `TwilioWhatsAppService.SendAlertAsync()` per listing
7. Each alert is logged to `alert_logs`

### Key design decisions
- **Composite PK on listings**: `(id, job_id)` — same OLX listing ID can appear under multiple jobs
- **`ON CONFLICT DO NOTHING`** in `db_writer.py` — deduplication is DB-level, not application-level
- **Playwright runs `headless=False`** with a 200×200 window minimized via CDP `Browser.setWindowBounds`. Headless mode is blocked by OLX on this machine (HTTP/2 protocol error). The CDP minimize call happens immediately after page creation.
- **OLX reference data** (`/api/olx/locations`, `/api/olx/categories`) is fetched via `HttpClient` named `"olx"` and cached in-process for 1 hour in `OlxReferenceController`

### Rate limiting (3 policies, keyed by client IP)
- `api` — 60 req/min on all `SearchJobsController` endpoints
- `scraper-run` — 5 req/min on `POST /api/search-jobs/{id}/run` (overrides `api`)
- `reference` — 20 req/min on `/api/olx/*`

### URL generation
`scraper/url_generator.py` always adds `sort=desc-creation` (newest listings first — critical for timely alerts) and optionally `category=<id>`. The `location` param accepts OLX numeric IDs: `1000001` = all India, state IDs (~2xxxxxx), city IDs (~4xxxxxx).

### Secrets
- `.env` — Docker Compose secrets (gitignored)
- `appsettings.Development.json` — local dev overrides (gitignored)
- `appsettings.json` has placeholder `Password=changeme`; real password stays in `.env` / env vars only
