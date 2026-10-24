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
