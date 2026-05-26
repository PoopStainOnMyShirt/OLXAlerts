import asyncio
import json
import argparse
from playwright.async_api import async_playwright
from url_generator import generate_start_url
from db_writer import save_listings


async def scrape_all_olx_pages(initial_url):
    all_listings = []

    async with async_playwright() as p:
        browser = await p.chromium.launch(
            headless=False,
            args=[
                "--no-sandbox",
                "--disable-dev-shm-usage",
                "--window-size=200,200",
                "--window-position=0,0",
                "--mute-audio",
            ]
        )
        context = await browser.new_context(
            viewport={'width': 200, 'height': 200},
        )

        page = await context.new_page()

        # Minimize window to taskbar via CDP
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
            "Sec-Fetch-Site": "same-site",
            "Sec-Fetch-Mode": "cors",
            "Sec-Fetch-Dest": "empty",
        })

        current_url = initial_url

        while current_url:
            print(f"Navigating to: {current_url[:100]}...")
            await page.goto(current_url, wait_until="networkidle")

            json_text = await page.inner_text("body")

            try:
                data = json.loads(json_text)
                listings = data.get("data", [])
                all_listings.extend(listings)
                print(f"Fetched {len(listings)} items. Total so far: {len(all_listings)}")

                metadata = data.get("metadata", {})
                current_url = metadata.get("next_page_url")

            except json.JSONDecodeError:
                print("End of data reached or request blocked.")
                break

        await browser.close()

    if all_listings:
        print(f"Scraped {len(all_listings)} listings total.")
    else:
        print("No data collected.")

    return all_listings


def main():
    parser = argparse.ArgumentParser(description='OLX scraper for OLXAlerts')
    parser.add_argument('--job-id', type=int, required=True, help='Search job ID')
    parser.add_argument('--search-term', required=True, help='Search term to scrape')
    parser.add_argument('--location', default='1000001', help='OLX location code')
    parser.add_argument('--category-id', type=int, default=None, help='OLX category ID (e.g. 84 for Cars)')
    args = parser.parse_args()

    start_url = generate_start_url(args.search_term, args.location, args.category_id)
    print(f"Using start URL: {start_url}")

    listings = asyncio.run(scrape_all_olx_pages(start_url))
    total = len(listings)
    inserted = 0

    if listings:
        inserted = save_listings(args.job_id, listings)

    # Machine-parseable result line for the .NET host
    print(f"SCRAPER_RESULT:inserted={inserted},total={total}")


if __name__ == "__main__":
    main()
