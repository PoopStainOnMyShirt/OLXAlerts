import axios from 'axios'
import type { SearchJob, CreateJobDto, UpdateJobDto } from '@/types'

const http = axios.create({ baseURL: '/api' })

export const api = {
  // Jobs
  getJobs: () =>
    http.get<SearchJob[]>('/search-jobs').then(r => r.data),

  getJob: (id: number) =>
    http.get<SearchJob>(`/search-jobs/${id}`).then(r => r.data),

  createJob: (dto: CreateJobDto) =>
    http.post<SearchJob>('/search-jobs', dto).then(r => r.data),

  updateJob: (id: number, dto: UpdateJobDto) =>
    http.put<SearchJob>(`/search-jobs/${id}`, dto).then(r => r.data),

  deleteJob: (id: number) =>
    http.delete(`/search-jobs/${id}`),

  runJob: (id: number) =>
    http.post<{ message: string }>(`/search-jobs/${id}/run`).then(r => r.data),

  // OLX reference
  getLocations: () =>
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    http.get<any>('/olx/locations').then(r => r.data),
}
