import { request } from './client'
import type { SaveTaskRequest, TaskDto, TaskItemStatus, TaskPriority } from './types'

/** Optional list filters, AND-combined. Leaving one out means "All". */
export interface TaskListFilters {
  projectId?: number
  status?: TaskItemStatus
  priority?: TaskPriority
}

/**
 * GET /api/tasks — newest first, filtered by the API (never on the client).
 * Only the filters that are set are sent. Rejects with ApiError 400 for an unknown
 * projectId or an invalid status/priority.
 */
export function listTasks(filters: TaskListFilters = {}): Promise<TaskDto[]> {
  const params = new URLSearchParams()
  if (filters.projectId !== undefined) params.set('projectId', String(filters.projectId))
  if (filters.status !== undefined) params.set('status', filters.status)
  if (filters.priority !== undefined) params.set('priority', filters.priority)
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

/** DELETE /api/tasks/{id} — resolves with no value (204); rejects with ApiError 404 for an unknown id. */
export function deleteTask(id: number): Promise<void> {
  return request<void>(`/api/tasks/${id}`, { method: 'DELETE' })
}
