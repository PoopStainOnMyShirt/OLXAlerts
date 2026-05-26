import { useState, useEffect } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Play, Pencil, Trash2, Loader2, MapPin, Clock, Timer, Tag } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { api } from '@/lib/api'
import { toast } from '@/hooks/use-toast'
import { formatPriceRange, formatInterval, relativeTime } from '@/lib/utils'
import type { SearchJob } from '@/types'

interface Props {
  job: SearchJob
  onEdit: (job: SearchJob) => void
  /** Called whenever a manual "Run now" scrape starts or finishes */
  onRunningChange?: (jobId: number, isRunning: boolean) => void
}

export function JobCard({ job, onEdit, onRunningChange }: Props) {
  const queryClient = useQueryClient()
  const [deleteConfirm, setDeleteConfirm] = useState(false)

  const runMutation = useMutation({
    mutationFn: () => api.runJob(job.id),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] })
      toast({ title: 'Scrape started', description: data.message })
    },
    onError: () => toast({ title: 'Run failed', description: 'Could not trigger scraper.', variant: 'destructive' }),
  })

  const deleteMutation = useMutation({
    mutationFn: () => api.deleteJob(job.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] })
      toast({ title: 'Job deleted' })
    },
    onError: () => toast({ title: 'Delete failed', variant: 'destructive' }),
  })

  // Notify parent whenever this card's scrape starts or finishes
  useEffect(() => {
    onRunningChange?.(job.id, runMutation.isPending)
  }, [runMutation.isPending]) // eslint-disable-line react-hooks/exhaustive-deps

  const priceRange = formatPriceRange(job.minPrice, job.maxPrice)

  // Resolve category name from static list (same source as CategorySelect)
  const CATEGORY_NAMES: Record<number, string> = {
    84: 'Cars', 81: 'Motorcycles', 1413: 'Scooters', 1415: 'Bicycles',
    85: 'Commercial Vehicles', 1725: 'For Sale: Houses', 1723: 'For Rent: Houses',
    1453: 'Mobile Phones', 1455: 'Tablets', 1505: 'Laptops', 1523: 'TVs & Audio',
    1417: 'Appliances', 1593: 'Sofas', 1591: 'Beds & Wardrobes',
  }
  const categoryName = job.categoryId ? (CATEGORY_NAMES[job.categoryId] ?? `Cat #${job.categoryId}`) : null

  return (
    <div className="group relative rounded-xl border bg-card p-5 shadow-sm transition-shadow hover:shadow-md flex flex-col gap-4">
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-base leading-tight truncate" title={job.searchTerm}>
            {job.searchTerm}
          </h3>
          <div className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
            <MapPin className="h-3 w-3 shrink-0" />
            <span className="truncate">{job.locationName ?? 'All India'}</span>
          </div>
        </div>
        <Badge variant={job.isActive ? 'success' : 'secondary'}>
          {job.isActive ? 'Active' : 'Paused'}
        </Badge>
      </div>

      {/* Metadata chips */}
      <div className="flex flex-wrap gap-2 text-xs">
        <span className="flex items-center gap-1 rounded-full bg-muted px-2.5 py-1 text-muted-foreground">
          <Timer className="h-3 w-3" />
          Every {formatInterval(job.intervalMinutes)}
        </span>
        {categoryName && (
          <span className="flex items-center gap-1 rounded-full bg-muted px-2.5 py-1 text-muted-foreground">
            <Tag className="h-3 w-3" />
            {categoryName}
          </span>
        )}
        {priceRange && (
          <span className="flex items-center gap-1 rounded-full bg-muted px-2.5 py-1 text-muted-foreground">
            {priceRange}
          </span>
        )}
      </div>

      {/* Timing */}
      <div className="flex gap-4 text-xs text-muted-foreground border-t pt-3">
        <div className="flex items-center gap-1">
          <Clock className="h-3 w-3" />
          <span>Last: {relativeTime(job.lastRunAt)}</span>
        </div>
        <div className="flex items-center gap-1">
          <Clock className="h-3 w-3" />
          <span>Next: {relativeTime(job.nextRunAt)}</span>
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2 pt-1">
        <Button
          size="sm"
          variant="outline"
          className="flex-1"
          onClick={() => runMutation.mutate()}
          disabled={runMutation.isPending}
        >
          {runMutation.isPending ? (
            <Loader2 className="h-3.5 w-3.5 animate-spin" />
          ) : (
            <Play className="h-3.5 w-3.5" />
          )}
          Run now
        </Button>
        <Button
          size="icon"
          variant="ghost"
          onClick={() => onEdit(job)}
          title="Edit job"
        >
          <Pencil className="h-4 w-4" />
        </Button>
        {deleteConfirm ? (
          <div className="flex gap-1">
            <Button
              size="sm"
              variant="destructive"
              onClick={() => deleteMutation.mutate()}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : 'Delete'}
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setDeleteConfirm(false)}>
              Cancel
            </Button>
          </div>
        ) : (
          <Button
            size="icon"
            variant="ghost"
            onClick={() => setDeleteConfirm(true)}
            title="Delete job"
            className="text-muted-foreground hover:text-destructive"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        )}
      </div>
    </div>
  )
}
