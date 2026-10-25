import { request } from './client'
import type { DashboardDto } from './types'

/** GET /api/dashboard — task totals, counted by the API on every call. */
export function getDashboard(): Promise<DashboardDto> {
  return request<DashboardDto>('/api/dashboard')
}
