import os
import json
import psycopg2
from psycopg2.extras import execute_values


def get_connection():
    return psycopg2.connect(
        host=os.environ.get('PGHOST', 'localhost'),
        port=os.environ.get('PGPORT', '5432'),
        dbname=os.environ.get('PGDATABASE', 'olxalerts'),
        user=os.environ.get('PGUSER', 'postgres'),
        password=os.environ.get('PGPASSWORD', ''),
    )


def save_listings(job_id, listings):
    """
    Upsert listings into the listings table.
    Returns count of newly inserted rows.
    """
    if not listings:
        return 0

    conn = get_connection()
    inserted = 0
    try:
        with conn.cursor() as cur:
            for item in listings:
                listing_id = item.get('id', '')
                if not listing_id:
                    continue

                title = item.get('title', '')
                user_name = item.get('user_name', '')
                description = item.get('description', '')
                olx_created_at = item.get('created_at', '')
                car_body_type = item.get('car_body_type', '')
                ad_id = item.get('ad_id', '')
                is_business = bool(item.get('is_business'))

                price = item.get('price', {}).get('value', {})
                price_display = price.get('display', '') if isinstance(price, dict) else ''
                price_raw = price.get('raw', None) if isinstance(price, dict) else None
                price_value = float(price_raw) if isinstance(price_raw, (int, float)) else None

                location = (
                    item.get('locations_resolved', {}).get('SUBLOCALITY_LEVEL_1_name', '')
                    or item.get('locations_resolved', {}).get('CITY_name', '')
                )
                raw_data = json.dumps(item, ensure_ascii=False)

                cur.execute(
                    """
                    INSERT INTO listings
                        (id, job_id, title, user_name, description, olx_created_at,
                         car_body_type, ad_id, is_business, price_display, price_value,
                         location, raw_data)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s::jsonb)
                    ON CONFLICT (id, job_id) DO NOTHING
                    """,
                    (
                        listing_id, job_id, title, user_name, description, olx_created_at,
                        car_body_type, ad_id, is_business, price_display, price_value,
                        location, raw_data,
                    )
                )
                inserted += cur.rowcount

        conn.commit()
    finally:
        conn.close()

    return inserted
