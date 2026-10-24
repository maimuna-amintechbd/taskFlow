import { request } from './client'

/** GET /api/health — resolves to the text "Healthy" when the API is up. */
export function getHealth(): Promise<string> {
  return request<string>('/api/health')
}
