import { useState, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Plus, Bell, Loader2, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { JobCard } from '@/components/JobCard'
import { JobFormModal } from '@/components/JobFormModal'
import { ScrapingStatusBar } from '@/components/ScrapingStatusBar'
import { Toaster } from '@/components/ui/toaster'
import { api } from '@/lib/api'
import type { SearchJob } from '@/types'

export default function App() {
  const [modalOpen, setModalOpen] = useState(false)
  const [editingJob, setEditingJob] = useState<SearchJob | null>(null)
  const [runningJobs, setRunningJobs] = useState<Map<number, string>>(new Map())

  const handleRunningChange = useCallback(
    (jobId: number, isRunning: boolean, searchTerm: string) => {
      setRunningJobs(prev => {
        const next = new Map(prev)
        if (isRunning) next.set(jobId, searchTerm)
        else next.delete(jobId)
        return next
      })
    },
    [],
  )

  const { data: jobs, isLoading, isError, error } = useQuery({
    queryKey: ['jobs'],
    queryFn: api.getJobs,
    refetchInterval: 30_000,
  })

  function handleNewJob() {
    setEditingJob(null)
    setModalOpen(true)
  }

  function handleEditJob(job: SearchJob) {
    setEditingJob(job)
    setModalOpen(true)
  }

  function handleModalClose(open: boolean) {
    setModalOpen(open)
    if (!open) setEditingJob(null)
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Header */}
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 sm:px-6">
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <Bell className="h-4 w-4" />
            </div>
            <div>
              <h1 className="text-base font-semibold leading-none">OLX Alerts</h1>
              <p className="text-xs text-muted-foreground">Marketplace monitor</p>
            </div>
          </div>
          <Button size="sm" onClick={handleNewJob}>
            <Plus className="h-4 w-4" />
            New Job
          </Button>
        </div>
      </header>

      {/* Main */}
      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6">
        {isLoading && (
          <div className="flex flex-col items-center justify-center py-24 gap-3 text-muted-foreground">
            <Loader2 className="h-8 w-8 animate-spin" />
            <span className="text-sm">Loading jobs…</span>
          </div>
        )}

        {isError && (
          <div className="flex flex-col items-center justify-center py-24 gap-3 text-destructive">
            <AlertCircle className="h-8 w-8" />
            <p className="text-sm font-medium">Failed to load jobs</p>
            <p className="text-xs text-muted-foreground">
              {error instanceof Error ? error.message : 'Unknown error'}
            </p>
          </div>
        )}

        {!isLoading && !isError && jobs?.length === 0 && (
          <div className="flex flex-col items-center justify-center py-24 gap-4 text-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-muted">
              <Bell className="h-7 w-7 text-muted-foreground" />
            </div>
            <div>
              <p className="text-base font-medium">No alert jobs yet</p>
              <p className="text-sm text-muted-foreground mt-1">
                Create your first job to start monitoring OLX listings
              </p>
            </div>
            <Button onClick={handleNewJob}>
              <Plus className="h-4 w-4" />
              Create your first job
            </Button>
          </div>
        )}

        {!isLoading && jobs && jobs.length > 0 && (
          <>
            <div className="mb-5 flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {jobs.length} {jobs.length === 1 ? 'job' : 'jobs'}
              </p>
            </div>
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {jobs.map(job => (
                <JobCard
                  key={job.id}
                  job={job}
                  onEdit={handleEditJob}
                  onRunningChange={(id, running) =>
                    handleRunningChange(id, running, job.searchTerm)
                  }
                />
              ))}
            </div>
          </>
        )}
      </main>

      <JobFormModal open={modalOpen} onOpenChange={handleModalClose} job={editingJob} />
      <ScrapingStatusBar
        count={runningJobs.size}
        labels={Array.from(runningJobs.values())}
      />
      <Toaster />
    </div>
  )
}
