# OLXAlerts

A scheduled marketplace alerts system for OLX India. Create search jobs, scrape listings automatically on a configurable interval, and receive WhatsApp notifications for new listings via Twilio.

## Architecture

```
REST API → PostgreSQL ← Python scraper (Playwright)
              ↓
   Twilio WhatsApp → client's phone
```

- **.NET 10 Web API** — manages jobs, triggers scraper, sends alerts
- **Python scraper** — Playwright-based, pages through OLX search results, writes directly to PostgreSQL
- **PostgreSQL** — shared DB; schema managed by EF Core migrations
- **Docker Compose** — one command to run the full stack

## Features

- Full CRUD for search jobs (search term, location, category, interval)
- Category and city-level filtering using OLX's own location/category IDs
- Results sorted newest-first (`sort=desc-creation`) — critical for timely alerts
- WhatsApp alerts via Twilio Sandbox with direct OLX listing links
- Deduplication via `ON CONFLICT DO NOTHING` — re-scraping never sends duplicate alerts
- Background scheduler polls every 30 seconds; `next_run_at` updated before each run to prevent double-fire
- Rate limiting: 60 req/min (CRUD), 5 req/min (manual trigger), 20 req/min (reference data)
- Browse valid location IDs (7,439 cities) and category IDs via `/api/olx/locations` and `/api/olx/categories`

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/search-jobs` | List all jobs |
| POST | `/api/search-jobs` | Create a job |
| GET | `/api/search-jobs/{id}` | Get job by ID |
| PUT | `/api/search-jobs/{id}` | Update job |
| DELETE | `/api/search-jobs/{id}` | Delete job (cascades) |
| POST | `/api/search-jobs/{id}/run` | Trigger scrape immediately |
| GET | `/api/search-jobs/{id}/listings` | Paginated listing history |
| GET | `/api/olx/locations` | Browse OLX location tree |
| GET | `/api/olx/categories` | Browse OLX categories |

Swagger UI available at `http://localhost:8080/swagger`.

## Quickstart (Docker)

```bash
cp .env.example .env
# Fill in TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, POSTGRES_PASSWORD
docker compose up --build
```

## Quickstart (local dev)

**Prerequisites:** .NET 10 SDK, Python 3.10+, PostgreSQL, Playwright Chromium

```bash
# Python scraper
cd scraper
pip install -r requirements.txt
python -m playwright install chromium

# .NET API
cd OLXAlerts.Api
dotnet run
# Swagger: https://localhost:5001/swagger
```

### Create a search job

```json
POST /api/search-jobs
{
  "searchTerm": "Honda City",
  "locationCode": "2001152",
  "locationName": "Delhi",
  "categoryId": 84,
  "whatsAppNumber": "+91XXXXXXXXXX",
  "intervalMinutes": 60
}
```

## Configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:Postgres` | Npgsql connection string |
| `Twilio:AccountSid` | Twilio account SID |
| `Twilio:AuthToken` | Twilio auth token |
| `Twilio:WhatsAppFrom` | Sender number (default: Twilio sandbox) |
| `Scraper:ScriptPath` | Absolute path to `scraper.py` |
| `Scraper:PythonBin` | Python executable (`python` / `python3`) |

## Database Schema

Three tables managed by EF Core migrations: `search_jobs`, `listings` (composite PK `id + job_id`), `alert_logs`.

## Common OLX Category IDs

| Category | ID |
|----------|----|
| Cars | 84 |
| Motorcycles | 81 |
| Scooters | 1413 |
| Mobile Phones | 1453 |
| For Sale: Houses & Apartments | 1725 |
| For Rent: Houses & Apartments | 1723 |

Use `GET /api/olx/categories` for the full list.
