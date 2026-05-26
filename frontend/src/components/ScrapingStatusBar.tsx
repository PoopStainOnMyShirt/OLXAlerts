import { Loader2 } from 'lucide-react'

interface Props {
  /** Number of jobs currently scraping */
  count: number
  /** All job labels (search terms) that are running */
  labels: string[]
}

export function ScrapingStatusBar({ count, labels }: Props) {
  if (count === 0) return null

  const label =
    count === 1
      ? `Scraping "${labels[0]}"…`
      : `Scraping ${count} jobs…`

  return (
    <div
      className="
        fixed bottom-4 right-4 z-50
        flex items-center gap-2.5
        rounded-full border bg-background/95 px-4 py-2.5
        shadow-lg backdrop-blur
        text-sm font-medium text-foreground
        animate-in slide-in-from-bottom-2 fade-in duration-300
      "
    >
      <Loader2 className="h-4 w-4 animate-spin text-primary shrink-0" />
      <span className="max-w-[240px] truncate">{label}</span>
    </div>
  )
}
