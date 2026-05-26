import { useEffect } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { LocationSearch } from '@/components/LocationSearch'
import { CategorySelect } from '@/components/CategorySelect'
import { api } from '@/lib/api'
import { toast } from '@/hooks/use-toast'
import type { SearchJob } from '@/types'
import { cn } from '@/lib/utils'

// ---------------------------------------------------------------------------
// Form schema
// ---------------------------------------------------------------------------
const schema = z.object({
  searchTerm: z.string().min(1, 'Item name is required').max(200),
  locationCode: z.string().default('1000001'),
  locationName: z.string().default('All India'),
  categoryId: z.number().nullable().optional(),
  telegramChatId: z
    .string()
    .min(1, 'Telegram Chat ID is required')
    .regex(/^-?\d+$/, 'Must be a numeric ID'),
  minPrice: z.string().optional(),
  maxPrice: z.string().optional(),
  intervalMinutes: z.number(),
  isActive: z.boolean().optional(),
})

type FormValues = z.infer<typeof schema>

const FREQUENCY_PRESETS = [
  { label: '15m', value: 15 },
  { label: '30m', value: 30 },
  { label: '1h', value: 60 },
  { label: '6h', value: 360 },
  { label: '12h', value: 720 },
  { label: '24h', value: 1440 },
]

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------
interface Props {
  open: boolean
  onOpenChange: (v: boolean) => void
  job?: SearchJob | null  // null = create mode, SearchJob = edit mode
}

export function JobFormModal({ open, onOpenChange, job }: Props) {
  const isEdit = !!job
  const queryClient = useQueryClient()

  const { register, handleSubmit, control, reset, setValue, watch, formState: { errors } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: {
        searchTerm: '',
        locationCode: '1000001',
        locationName: 'All India',
        categoryId: null,
        telegramChatId: '',
        minPrice: '',
        maxPrice: '',
        intervalMinutes: 60,
        isActive: true,
      },
    })

  // Populate form when editing
  useEffect(() => {
    if (job) {
      reset({
        searchTerm: job.searchTerm,
        locationCode: job.locationCode,
        locationName: job.locationName ?? 'All India',
        categoryId: job.categoryId ?? null,
        telegramChatId: job.telegramChatId?.toString() ?? '',
        minPrice: job.minPrice?.toString() ?? '',
        maxPrice: job.maxPrice?.toString() ?? '',
        intervalMinutes: job.intervalMinutes,
        isActive: job.isActive,
      })
    } else {
      reset({
        searchTerm: '',
        locationCode: '1000001',
        locationName: 'All India',
        categoryId: null,
        telegramChatId: '',
        minPrice: '',
        maxPrice: '',
        intervalMinutes: 60,
        isActive: true,
      })
    }
  }, [job, reset, open])

  const createMutation = useMutation({
    mutationFn: api.createJob,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] })
      toast({ title: 'Job created', description: 'Alert job is now active.' })
      onOpenChange(false)
    },
    onError: () => toast({ title: 'Error', description: 'Failed to create job.', variant: 'destructive' }),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: Parameters<typeof api.updateJob>[1] }) =>
      api.updateJob(id, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['jobs'] })
      toast({ title: 'Job updated' })
      onOpenChange(false)
    },
    onError: () => toast({ title: 'Error', description: 'Failed to update job.', variant: 'destructive' }),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  function onSubmit(values: FormValues) {
    const minPrice = values.minPrice ? parseFloat(values.minPrice) : undefined
    const maxPrice = values.maxPrice ? parseFloat(values.maxPrice) : undefined
    const telegramChatId = parseInt(values.telegramChatId, 10)

    if (isEdit && job) {
      updateMutation.mutate({
        id: job.id,
        dto: {
          searchTerm: values.searchTerm,
          locationCode: values.locationCode,
          locationName: values.locationName,
          categoryId: values.categoryId ?? undefined,
          telegramChatId,
          minPrice: minPrice ?? null,
          maxPrice: maxPrice ?? null,
          intervalMinutes: values.intervalMinutes,
          isActive: values.isActive,
          notificationChannel: 1,
        },
      })
    } else {
      createMutation.mutate({
        searchTerm: values.searchTerm,
        locationCode: values.locationCode,
        locationName: values.locationName,
        categoryId: values.categoryId ?? undefined,
        notificationChannel: 1,
        telegramChatId,
        minPrice,
        maxPrice,
        intervalMinutes: values.intervalMinutes,
      })
    }
  }

  const selectedInterval = watch('intervalMinutes')

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Edit Job' : 'New Alert Job'}</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-1">
          {/* Item name */}
          <div className="space-y-1.5">
            <Label htmlFor="searchTerm">
              Item Name <span className="text-destructive">*</span>
            </Label>
            <Input
              id="searchTerm"
              placeholder="e.g. Honda City, iPhone 14"
              autoFocus
              {...register('searchTerm')}
            />
            {errors.searchTerm && (
              <p className="text-xs text-destructive">{errors.searchTerm.message}</p>
            )}
          </div>

          {/* Location */}
          <div className="space-y-1.5">
            <Label>Location</Label>
            <Controller
              control={control}
              name="locationCode"
              render={({ field }) => (
                <LocationSearch
                  value={field.value}
                  displayValue={watch('locationName') ?? ''}
                  onChange={(code, name) => {
                    field.onChange(code)
                    setValue('locationName', name)
                  }}
                />
              )}
            />
          </div>

          {/* Category */}
          <div className="space-y-1.5">
            <Label>Category</Label>
            <Controller
              control={control}
              name="categoryId"
              render={({ field }) => (
                <CategorySelect
                  value={field.value ?? null}
                  onChange={id => field.onChange(id)}
                />
              )}
            />
          </div>

          {/* Price range */}
          <div className="space-y-1.5">
            <Label>Price Range (₹)</Label>
            <div className="flex items-center gap-2">
              <div className="relative flex-1">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground">₹</span>
                <Input
                  type="number"
                  placeholder="Min"
                  className="pl-7"
                  min={0}
                  {...register('minPrice')}
                />
              </div>
              <span className="text-muted-foreground text-sm">–</span>
              <div className="relative flex-1">
                <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-muted-foreground">₹</span>
                <Input
                  type="number"
                  placeholder="Max"
                  className="pl-7"
                  min={0}
                  {...register('maxPrice')}
                />
              </div>
            </div>
          </div>

          {/* Frequency */}
          <div className="space-y-1.5">
            <Label>Scrape Frequency</Label>
            <Controller
              control={control}
              name="intervalMinutes"
              render={({ field }) => (
                <div className="flex gap-1.5 flex-wrap">
                  {FREQUENCY_PRESETS.map(p => (
                    <button
                      key={p.value}
                      type="button"
                      onClick={() => field.onChange(p.value)}
                      className={cn(
                        'flex-1 min-w-[52px] rounded-md border px-3 py-1.5 text-sm font-medium transition-colors',
                        selectedInterval === p.value
                          ? 'bg-primary text-primary-foreground border-primary'
                          : 'bg-background hover:bg-accent border-input',
                      )}
                    >
                      {p.label}
                    </button>
                  ))}
                </div>
              )}
            />
          </div>

          {/* Telegram Chat ID */}
          <div className="space-y-1.5">
            <Label htmlFor="telegramChatId">
              Telegram Chat ID <span className="text-destructive">*</span>
            </Label>
            <Input
              id="telegramChatId"
              placeholder="-1001234567890"
              {...register('telegramChatId')}
            />
            {errors.telegramChatId && (
              <p className="text-xs text-destructive">{errors.telegramChatId.message}</p>
            )}
            <p className="text-xs text-muted-foreground">
              Use @userinfobot to get your chat ID
            </p>
          </div>

          {/* Active toggle (edit only) */}
          {isEdit && (
            <div className="flex items-center justify-between rounded-lg border p-3">
              <div>
                <p className="text-sm font-medium">Active</p>
                <p className="text-xs text-muted-foreground">Pause to stop scheduled scrapes</p>
              </div>
              <Controller
                control={control}
                name="isActive"
                render={({ field }) => (
                  <Switch
                    checked={field.value ?? true}
                    onCheckedChange={field.onChange}
                  />
                )}
              />
            </div>
          )}

          <DialogFooter className="pt-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isEdit ? 'Save Changes' : 'Create Job'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
