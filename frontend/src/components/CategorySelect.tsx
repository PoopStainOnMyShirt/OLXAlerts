import { useState } from 'react'
import { ChevronDown } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'

// ---------------------------------------------------------------------------
// Static category list sourced from GET /api/olx/categories (OLX India)
// Groups map to parent categories; items map to sub_categories.
// ---------------------------------------------------------------------------
const CATEGORY_GROUPS = [
  {
    label: 'Vehicles',
    items: [
      { id: 84,   name: 'Cars' },
      { id: 81,   name: 'Motorcycles' },
      { id: 1413, name: 'Scooters' },
      { id: 1415, name: 'Bicycles' },
      { id: 85,   name: 'Commercial & Other Vehicles' },
      { id: 1587, name: 'Spare Parts' },
    ],
  },
  {
    label: 'Properties',
    items: [
      { id: 1725, name: 'For Sale: Houses & Apartments' },
      { id: 1723, name: 'For Rent: Houses & Apartments' },
      { id: 301,  name: 'For Sale: New Projects & Properties' },
      { id: 1729, name: 'Lands & Plots' },
      { id: 1731, name: 'For Rent: Shops & Offices' },
      { id: 1733, name: 'For Sale: Shops & Offices' },
      { id: 1449, name: 'PG & Guest Houses' },
    ],
  },
  {
    label: 'Mobiles',
    items: [
      { id: 1453, name: 'Mobile Phones' },
      { id: 1455, name: 'Tablets' },
      { id: 1457, name: 'Accessories' },
    ],
  },
  {
    label: 'Electronics & Appliances',
    items: [
      { id: 1505, name: 'Computers & Laptops' },
      { id: 1523, name: 'TVs, Video & Audio' },
      { id: 1515, name: 'Computer Accessories' },
      { id: 1517, name: 'Cameras & Lenses' },
      { id: 1417, name: 'Kitchen & Other Appliances' },
      { id: 1617, name: 'Fridges' },
      { id: 1619, name: 'ACs' },
      { id: 1615, name: 'Washing Machines' },
      { id: 93,   name: 'Games & Entertainment' },
    ],
  },
  {
    label: 'Furniture',
    items: [
      { id: 1593, name: 'Sofa & Dining' },
      { id: 1591, name: 'Beds & Wardrobes' },
      { id: 575,  name: 'Home Decor & Garden' },
      { id: 293,  name: 'Other Household Items' },
    ],
  },
  {
    label: 'Fashion',
    items: [
      { id: 1793, name: 'Men' },
      { id: 1795, name: 'Women' },
      { id: 235,  name: 'Kids' },
    ],
  },
  {
    label: 'Books, Sports & Hobbies',
    items: [
      { id: 453, name: 'Books' },
      { id: 771, name: 'Gym & Fitness' },
      { id: 714, name: 'Musical Instruments' },
      { id: 100, name: 'Sports Equipment' },
    ],
  },
  {
    label: 'Pets',
    items: [
      { id: 139, name: 'Dogs' },
      { id: 140, name: 'Other Pets' },
      { id: 175, name: 'Pet Food & Accessories' },
    ],
  },
  {
    label: 'Services',
    items: [
      { id: 1429, name: 'Education & Classes' },
      { id: 741,  name: 'Health & Beauty' },
      { id: 1301, name: 'Home Renovation & Repair' },
      { id: 1304, name: 'Packers & Movers' },
      { id: 625,  name: 'Other Services' },
    ],
  },
]

// Flat lookup map: id → display name
const ID_TO_NAME = new Map<number, string>(
  CATEGORY_GROUPS.flatMap(g => g.items.map(i => [i.id, `${i.name} — ${g.label}`] as [number, string])),
)

const ALL_ITEMS = CATEGORY_GROUPS.flatMap(g =>
  g.items.map(i => ({ ...i, group: g.label })),
)

interface Props {
  value?: number | null
  onChange: (id: number | null) => void
}

export function CategorySelect({ value, onChange }: Props) {
  const [open, setOpen]   = useState(false)
  const [query, setQuery] = useState('')
  const [showCustom, setShowCustom] = useState(false)
  const [customId, setCustomId]     = useState('')

  const filtered = query.trim()
    ? ALL_ITEMS.filter(
        i =>
          i.name.toLowerCase().includes(query.toLowerCase()) ||
          i.group.toLowerCase().includes(query.toLowerCase()),
      )
    : null // show grouped view when no query

  const selectedName = value ? (ID_TO_NAME.get(value) ?? `Category ${value}`) : null

  function handleSelect(id: number | null) {
    onChange(id)
    setOpen(false)
    setQuery('')
    setShowCustom(false)
  }

  function handleCustomSubmit() {
    const n = parseInt(customId.trim(), 10)
    if (!isNaN(n) && n > 0) {
      onChange(n)
      setOpen(false)
      setQuery('')
      setShowCustom(false)
      setCustomId('')
    }
  }

  return (
    <div className="relative w-full">
      <div
        className={cn(
          'flex h-9 w-full cursor-pointer items-center rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors',
          open && 'ring-1 ring-ring',
        )}
        onClick={() => setOpen(v => !v)}
      >
        <span className={cn('flex-1 truncate', !selectedName && 'text-muted-foreground')}>
          {selectedName ?? 'All categories'}
        </span>
        {value && (
          <button
            type="button"
            onClick={e => { e.stopPropagation(); handleSelect(null) }}
            className="mr-1 rounded-full p-0.5 text-xs text-muted-foreground hover:bg-muted"
          >
            ✕
          </button>
        )}
        <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
      </div>

      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-md">
          {/* Search input */}
          <div className="border-b p-2">
            <input
              autoFocus
              value={query}
              onChange={e => setQuery(e.target.value)}
              placeholder="Search category…"
              className="w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground"
            />
          </div>

          <ul className="max-h-64 overflow-y-auto py-1">
            {/* All categories option */}
            <li
              onClick={() => handleSelect(null)}
              className={cn(
                'cursor-pointer px-3 py-2 text-sm hover:bg-accent',
                !value && 'bg-accent',
              )}
            >
              All categories
            </li>

            {/* Filtered flat list */}
            {filtered
              ? filtered.map(item => (
                  <li
                    key={item.id}
                    onClick={() => handleSelect(item.id)}
                    className={cn(
                      'cursor-pointer px-3 py-2 text-sm hover:bg-accent',
                      value === item.id && 'bg-accent',
                    )}
                  >
                    <span>{item.name}</span>
                    <span className="ml-1.5 text-xs text-muted-foreground">— {item.group}</span>
                  </li>
                ))
              : /* Grouped view */
                CATEGORY_GROUPS.map(group => (
                  <li key={group.label}>
                    <div className="px-3 pb-0.5 pt-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                      {group.label}
                    </div>
                    <ul>
                      {group.items.map(item => (
                        <li
                          key={item.id}
                          onClick={() => handleSelect(item.id)}
                          className={cn(
                            'cursor-pointer px-4 py-1.5 text-sm hover:bg-accent',
                            value === item.id && 'bg-accent',
                          )}
                        >
                          {item.name}
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}

            {/* Custom ID entry */}
            <li className="border-t">
              {showCustom ? (
                <div className="flex items-center gap-2 px-3 py-2">
                  <Input
                    type="number"
                    placeholder="Enter category ID"
                    value={customId}
                    onChange={e => setCustomId(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && handleCustomSubmit()}
                    className="h-7 text-xs"
                    autoFocus
                  />
                  <button
                    type="button"
                    onClick={handleCustomSubmit}
                    className="text-xs font-medium text-primary hover:underline"
                  >
                    OK
                  </button>
                </div>
              ) : (
                <div
                  onClick={() => setShowCustom(true)}
                  className="cursor-pointer px-3 py-2 text-sm text-muted-foreground hover:bg-accent"
                >
                  Enter ID manually…
                </div>
              )}
            </li>
          </ul>
        </div>
      )}
    </div>
  )
}
