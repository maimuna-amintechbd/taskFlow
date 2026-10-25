import { request } from './client'
import type { SaveTaskRequest, TaskDto } from './types'

/** Optional list filters. Leaving one out means "All". Story 4.1 adds status and priority. */
export interface TaskListFilters {
  projectId?: number
}

/** GET /api/tasks — newest first; rejects with ApiError 400 for an unknown projectId. */
export function listTasks(filters: TaskListFilters = {}): Promise<TaskDto[]> {
  const params = new URLSearchParams()
  if (filters.projectId !== undefined) params.set('projectId', String(filters.projectId))
  const query = params.toString()
  return request<TaskDto[]>(query === '' ? '/api/tasks' : `/api/tasks?${query}`)
}

/** POST /api/tasks — rejects with ApiError (status 400, fieldErrors) when invalid. */
export function createTask(body: SaveTaskRequest): Promise<TaskDto> {
  return request<TaskDto>('/api/tasks', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

/** GET /api/tasks/{id} — rejects with ApiError (status 404) for an unknown id. */
export function getTask(id: number): Promise<TaskDto> {
  return request<TaskDto>(`/api/tasks/${id}`)
}

/** PUT /api/tasks/{id} — full replace; rejects with ApiError 404 (unknown id) or 400 (invalid). */
export function updateTask(id: number, body: SaveTaskRequest): Promise<TaskDto> {
  return request<TaskDto>(`/api/tasks/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
