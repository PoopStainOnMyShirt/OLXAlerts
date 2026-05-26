import { useState, useMemo, useRef, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronDown, MapPin, X } from 'lucide-react'
import { api } from '@/lib/api'
import { cn } from '@/lib/utils'
import type { OlxLocation } from '@/types'

// ---------------------------------------------------------------------------
// Flatten the OLX location tree into a searchable flat list.
//
// Real API shape (confirmed):
// {
//   "data": [{
//     "id": 1000001, "name": "India", "type": "COUNTRY",
//     "children": [{
//       "id": 2001145, "name": "Andhra Pradesh", "type": "STATE",
//       "children": [{ "id": 4058501, "name": "Adoni", "type": "CITY" }]
//     }]
//   }]
// }
// ---------------------------------------------------------------------------
// eslint-disable-next-line @typescript-eslint/no-explicit-any
function flattenLocations(raw: any): OlxLocation[] {
  const results: OlxLocation[] = []
  // "All India" is always the first option
  results.push({ id: '1000001', name: 'All India', path: 'All India', level: 'country' })

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  function walk(nodes: any[], breadcrumb: string[]) {
    if (!Array.isArray(nodes)) return
    for (const node of nodes) {
      const id   = String(node.id ?? '')
      const name = String(node.name ?? node.label ?? '')
      const type = String(node.type ?? '').toUpperCase()
      const children: unknown[] = Array.isArray(node.children) ? node.children : []

      if (!id || !name) continue

      if (type === 'COUNTRY') {
        // Don't add the country node — "All India" is already first.
        // Recurse into states.
        walk(children as never[], [])
        continue
      }

      const pathParts = [...breadcrumb, name]
      const path = pathParts.join(' › ')
      const level: OlxLocation['level'] = type === 'STATE' ? 'state' : 'city'
      results.push({ id, name, path, level })

      if (children.length > 0) {
        walk(children as never[], pathParts)
      }
    }
  }

  const nodes = raw?.data ?? raw
  walk(Array.isArray(nodes) ? nodes : [nodes], [])

  return results
}

interface Props {
  value: string
  displayValue: string
  onChange: (code: string, name: string) => void
}

export function LocationSearch({ value, displayValue, onChange }: Props) {
  const [open, setOpen]   = useState(false)
  const [query, setQuery] = useState('')
  const inputRef          = useRef<HTMLInputElement>(null)
  const containerRef      = useRef<HTMLDivElement>(null)

  const { data: rawLocations, isLoading, isError } = useQuery({
    queryKey: ['locations'],
    queryFn: api.getLocations,
    staleTime: 60 * 60 * 1000, // matches server-side 1-hour cache
    retry: false,
  })

  const locations = useMemo(
    () => (rawLocations ? flattenLocations(rawLocations) : []),
    [rawLocations],
  )

  const filtered = useMemo(() => {
    if (!query.trim()) return locations.slice(0, 50)
    const q = query.toLowerCase()
    return locations
      .filter(l => l.name.toLowerCase().includes(q) || l.path.toLowerCase().includes(q))
      .slice(0, 50)
  }, [locations, query])

  // Close on outside click
  useEffect(() => {
    function handle(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handle)
    return () => document.removeEventListener('mousedown', handle)
  }, [])

  function handleSelect(loc: OlxLocation) {
    onChange(loc.id, loc.name)
    setQuery('')
    setOpen(false)
  }

  function handleClear(e: React.MouseEvent) {
    e.stopPropagation()
    onChange('1000001', 'All India')
    setQuery('')
  }

  const hasCustomValue = value && value !== '1000001'

  return (
    <div ref={containerRef} className="relative w-full">
      <div
        className={cn(
          'flex h-9 w-full cursor-pointer items-center rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors',
          open && 'ring-1 ring-ring',
        )}
        onClick={() => { setOpen(v => !v); setTimeout(() => inputRef.current?.focus(), 50) }}
      >
        <MapPin className="mr-2 h-3.5 w-3.5 shrink-0 text-muted-foreground" />
        <span className={cn('flex-1 truncate', !displayValue && 'text-muted-foreground')}>
          {displayValue || 'Search location…'}
        </span>
        {hasCustomValue && (
          <button type="button" onClick={handleClear} className="mr-1 rounded-full p-0.5 hover:bg-muted">
            <X className="h-3 w-3" />
          </button>
        )}
        <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
      </div>

      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-md">
          <div className="border-b p-2">
            <input
              ref={inputRef}
              value={query}
              onChange={e => setQuery(e.target.value)}
              placeholder="Type city or state…"
              className="w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground"
            />
          </div>
          <ul className="max-h-60 overflow-y-auto py-1">
            {isLoading && (
              <li className="px-3 py-2 text-sm text-muted-foreground">Loading locations…</li>
            )}
            {isError && (
              <li className="px-3 py-2 text-sm text-destructive">Failed to load locations</li>
            )}
            {!isLoading && !isError && filtered.length === 0 && (
              <li className="px-3 py-2 text-sm text-muted-foreground">No locations found</li>
            )}
            {!isLoading && filtered.map(loc => (
              <li
                key={loc.id}
                onClick={() => handleSelect(loc)}
                className={cn(
                  'flex cursor-pointer items-center justify-between px-3 py-2 text-sm hover:bg-accent hover:text-accent-foreground',
                  loc.id === value && 'bg-accent',
                )}
              >
                <div>
                  <span className="font-medium">{loc.name}</span>
                  {loc.level !== 'country' && loc.path !== loc.name && (
                    <span className="ml-1.5 text-xs text-muted-foreground">{loc.path}</span>
                  )}
                </div>
                {loc.level === 'state' && (
                  <span className="text-xs italic text-muted-foreground">incl. all cities</span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  )
}
