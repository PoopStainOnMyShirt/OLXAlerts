# OLXAlerts

A scheduled marketplace alerts system for OLX India. Create search jobs via a React dashboard, scrape listings automatically on a configurable interval, and receive Telegram notifications for new listings.

<img width="1918" height="1030" alt="image" src="https://github.com/user-attachments/assets/4d3f8b63-deb4-4ff7-94aa-00dad6e8b702" />

## Architecture

```
React Dashboard → REST API → PostgreSQL ← Python scraper (Playwright)
                                 ↓
                         Telegram → user's phone
```

- **React frontend** (`/frontend`) — Vite + shadcn/ui dashboard for managing alert jobs
- **.NET 10 Web API** — manages jobs, triggers scraper, sends alerts
- **Python scraper** — Playwright-based, pages through OLX search results, writes directly to PostgreSQL
- **PostgreSQL** — shared DB; schema managed by EF Core migrations
- **Docker Compose** — one command to run the full stack

## Features

- **Dashboard UI** — create, edit, pause/resume, and manually trigger jobs from the browser
- Live scraping progress indicator (bottom-right floating badge while a run is in flight)
- Full CRUD for search jobs (search term, location, category, price range, interval)
- City and state-level location search — selecting a state includes all cities within it
- Price range filtering passed through to OLX search (`price_min` / `price_max`)
- Results sorted newest-first (`sort=desc-creation`) — critical for timely alerts
- Telegram alerts with direct OLX listing links
- Deduplication via `ON CONFLICT DO NOTHING` — re-scraping never sends duplicate alerts
- Background scheduler polls every 30 seconds; `next_run_at` updated before each run to prevent double-fire
- Anti-scraping rate limits: 2–5 s between paginated requests, 15–30 s stagger between jobs, 1–2 s between Telegram notifications
- API rate limiting: 60 req/min (CRUD), 5 req/min (manual trigger), 20 req/min (reference data)
- Browse valid location IDs (7,439 cities) and category IDs via `/api/olx/locations` and `/api/olx/categories`

## Quickstart (local dev)

**Prerequisites:** .NET 10 SDK, Python 3.10+, PostgreSQL, Playwright Chromium, Node.js 18+

```bash
# 1. Python scraper setup (once)
cd scraper && pip install -r requirements.txt && python -m playwright install chromium

# 2. Install Node deps (once)
npm install && npm install --prefix frontend

# 3. Start API + dashboard together
npm run dev
# API + Swagger: http://localhost:5184/swagger
# Dashboard:    http://localhost:5173
```

Or in VS Code: **Terminal → Run Build Task** (`Ctrl+Shift+B`) — opens both in dedicated terminal panels.

To run separately:
```bash
npm run dev:api   # .NET API only
npm run dev:ui    # React dashboard only
```

## Quickstart (Docker)

```bash
cp .env.example .env
# Fill in TELEGRAM_BOT_TOKEN, POSTGRES_PASSWORD
docker compose up --build
```

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

### Create a search job (API example)

```json
POST /api/search-jobs
{
  "searchTerm": "Honda City",
  "locationCode": "2001152",
  "locationName": "Delhi",
  "categoryId": 84,
  "notificationChannel": 1,
  "telegramChatId": -1001234567890,
  "minPrice": 300000,
  "maxPrice": 700000,
  "intervalMinutes": 60
}
```

## Configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:Postgres` | Npgsql connection string |
| `Telegram:BotToken` | Telegram bot token |
| `Scraper:ScriptPath` | Absolute path to `scraper.py` |
| `Scraper:PythonBin` | Python executable (`python` / `python3`) |

## Database Schema

Three tables managed by EF Core migrations: `search_jobs`, `listings` (composite PK `id + job_id`), `alert_logs`.

**`search_jobs` key fields:** `search_term`, `location_code`, `location_name`, `category_id`, `min_price`, `max_price`, `notification_channel`, `telegram_chat_id`, `interval_minutes`, `is_active`, `last_run_at`, `next_run_at`

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
