import urllib.parse
import constants

def generate_start_url(query, location='1000001', category_id=None):
    base_url = constants.BASE_URL_OLX + constants.RELEVANCE_SEARCH + constants.VERSION + 'search'
    params = {
        'facet_limit': constants.DEFAULT_FACET_LIMIT,
        'location': location,
        'location_facet_limit': '999',
        'paidListing': 'true',
        'platform': constants.DEFAULT_PLATFORM,
        'pttEnabled': constants.DEFAULT_PTT_ENABLED,
        'query': query,
        'relaxedFilters': constants.DEFAULT_RELAXED_FILTERS,
        'size': constants.DEFAULT_SIZE,
        'sort': 'desc-creation',
        'spellcheck': constants.DEFAULT_SPELLCHECK,
        'user': constants.DEFAULT_USER,
        'lang': constants.DEFAULT_LANG,
    }
    if category_id is not None:
        params['category'] = category_id
    query_string = '&'.join(f'{k}={urllib.parse.quote(str(v))}' for k, v in params.items())
    return base_url + '?' + query_string

# Example usage
if __name__ == "__main__":
    query = input("Enter search term: ").strip()
    if not query:
        print("Search term is required")
    else:
        print(generate_start_url(query))
