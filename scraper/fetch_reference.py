"""
Fetches an OLX reference API URL (locations or categories) via Playwright,
bypassing OLX's anti-bot protection that blocks plain HTTP clients.

Usage:
  python fetch_reference.py --url https://www.olx.in/api/locations/
  python fetch_reference.py --url https://www.olx.in/api/categories/

Prints the raw JSON body to stdout. Exits non-zero on failure.
"""

import asyncio
import argparse
import sys
from playwright.async_api import async_playwright


async def fetch_json(url: str) -> str:
    async with async_playwright() as p:
        browser = await p.chromium.launch(
            headless=False,
            args=[
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--window-size=200,200",
                "--window-position=0,0",
                "--mute-audio",
            ],
        )
        context = await browser.new_context(
            viewport={"width": 200, "height": 200},
        )
        page = await context.new_page()

        # Minimise window immediately via CDP (same trick as scraper.py)
        cdp = await context.new_cdp_session(page)
        window = await cdp.send("Browser.getWindowForTarget")
        await cdp.send("Browser.setWindowBounds", {
            "windowId": window["windowId"],
            "bounds": {"windowState": "minimized"},
        })

        await page.set_extra_http_headers({
            "Accept": "application/json, text/plain, */*",
            "Accept-Language": "en-IN,en-US;q=0.9,en;q=0.8",
            "Origin": "https://www.olx.in",
            "Referer": "https://www.olx.in/",
        })

        await page.goto(url, wait_until="networkidle", timeout=30_000)
        body = await page.inner_text("body")
        await browser.close()
        return body


def main():
    parser = argparse.ArgumentParser(description="Fetch OLX reference JSON via Playwright")
    parser.add_argument("--url", required=True, help="OLX API URL to fetch")
    args = parser.parse_args()

    try:
        result = asyncio.run(fetch_json(args.url))
        sys.stdout.buffer.write(result.encode("utf-8"))
        sys.stdout.buffer.write(b"\n")
        sys.stdout.buffer.flush()
    except Exception as e:
        print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
