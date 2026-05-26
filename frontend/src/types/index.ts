export type NotificationChannel = 0 | 1 // 0 = WhatsApp, 1 = Telegram

export interface SearchJob {
  id: number
  searchTerm: string
  locationCode: string
  locationName?: string
  categoryId?: number
  notificationChannel: NotificationChannel
  whatsAppNumber?: string
  telegramChatId?: number
  minPrice?: number
  maxPrice?: number
  intervalMinutes: number
  isActive: boolean
  createdAt: string
  lastRunAt?: string
  nextRunAt: string
}

export interface CreateJobDto {
  searchTerm: string
  locationCode: string
  locationName?: string
  categoryId?: number | null
  notificationChannel: 1 // always Telegram for now
  telegramChatId: number
  minPrice?: number
  maxPrice?: number
  intervalMinutes: number
}

export interface UpdateJobDto {
  searchTerm?: string
  locationCode?: string
  locationName?: string
  categoryId?: number | null
  notificationChannel?: NotificationChannel
  telegramChatId?: number
  minPrice?: number | null
  maxPrice?: number | null
  intervalMinutes?: number
  isActive?: boolean
}

export interface OlxLocation {
  id: string
  name: string
  path: string   // display breadcrumb e.g. "Maharashtra › Mumbai"
  level: 'country' | 'state' | 'city'
}
