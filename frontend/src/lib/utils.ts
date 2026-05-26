import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/** Format relative time: "2 min ago", "just now", "in 5 min" */
export function relativeTime(dateStr: string | undefined | null): string {
  if (!dateStr) return '—'
  const date = new Date(dateStr)
  const now = Date.now()
  const diffMs = date.getTime() - now
  const absDiff = Math.abs(diffMs)
  const mins = Math.floor(absDiff / 60_000)
  const hrs = Math.floor(absDiff / 3_600_000)
  const days = Math.floor(absDiff / 86_400_000)

  const past = diffMs < 0

  if (absDiff < 60_000) return 'just now'
  if (mins < 60) return past ? `${mins}m ago` : `in ${mins}m`
  if (hrs < 24) return past ? `${hrs}h ago` : `in ${hrs}h`
  return past ? `${days}d ago` : `in ${days}d`
}

/** Format price range for display: "₹50k – ₹2L" */
export function formatPriceRange(min?: number, max?: number): string | null {
  if (!min && !max) return null
  const fmt = (n: number) => {
    if (n >= 100_000) return `₹${(n / 100_000).toFixed(n % 100_000 === 0 ? 0 : 1)}L`
    if (n >= 1_000) return `₹${(n / 1_000).toFixed(n % 1_000 === 0 ? 0 : 1)}k`
    return `₹${n}`
  }
  if (min && max) return `${fmt(min)} – ${fmt(max)}`
  if (min) return `≥ ${fmt(min)}`
  return `≤ ${fmt(max!)}`
}

/** Format interval in minutes to human string */
export function formatInterval(mins: number): string {
  if (mins < 60) return `${mins} min`
  if (mins < 1440) return `${mins / 60} hr`
  return `${mins / 1440} day`
}
