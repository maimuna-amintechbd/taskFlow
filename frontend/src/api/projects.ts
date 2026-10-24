import { request } from './client'
import type { ProjectDto, SaveProjectRequest } from './types'

/** GET /api/projects — every project, ordered by name then id. */
export function listProjects(): Promise<ProjectDto[]> {
  return request<ProjectDto[]>('/api/projects')
}

/** POST /api/projects — rejects with ApiError (status 400, fieldErrors) when invalid. */
export function createProject(body: SaveProjectRequest): Promise<ProjectDto> {
  return request<ProjectDto>('/api/projects', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

/** GET /api/projects/{id} — rejects with ApiError (status 404) for an unknown id. */
export function getProject(id: number): Promise<ProjectDto> {
  return request<ProjectDto>(`/api/projects/${id}`)
}

/** PUT /api/projects/{id} — full replace; rejects with ApiError 404 (unknown id) or 400 (invalid). */
export function updateProject(id: number, body: SaveProjectRequest): Promise<ProjectDto> {
  return request<ProjectDto>(`/api/projects/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

/** DELETE /api/projects/{id} — resolves with no value (204); rejects with ApiError 404 for an unknown id. */
export function deleteProject(id: number): Promise<void> {
  return request<void>(`/api/projects/${id}`, { method: 'DELETE' })
}
