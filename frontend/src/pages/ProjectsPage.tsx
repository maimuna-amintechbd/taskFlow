import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router'
import { ApiError, FALLBACK_MESSAGE, messageOf, type FieldErrors } from '../api/client'
import { createProject, listProjects } from '../api/projects'
import type { ProjectDto, SaveProjectRequest } from '../api/types'
import EmptyState from '../components/EmptyState'
import ErrorBanner from '../components/ErrorBanner'
import ProjectForm from '../components/ProjectForm'
import { splitProjectFieldErrors } from '../components/projectFormFields'

type ListState =
  | { kind: 'loading' }
  | { kind: 'loaded'; projects: ProjectDto[] }
  | { kind: 'error'; message: string }

export default function ProjectsPage() {
  const [list, setList] = useState<ListState>({ kind: 'loading' })
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)

  const loadProjects = useCallback(async () => {
    try {
      setList({ kind: 'loaded', projects: await listProjects() })
    } catch (error) {
      setList({ kind: 'error', message: messageOf(error) })
    }
  }, [])

  useEffect(() => {
    // Ignore a late answer if the page was left before the request finished.
    let ignore = false
    listProjects()
      .then((projects) => {
        if (!ignore) setList({ kind: 'loaded', projects })
      })
      .catch((error: unknown) => {
        if (!ignore) setList({ kind: 'error', message: messageOf(error) })
      })
    return () => {
      ignore = true
    }
  }, [])

  async function handleCreate(values: SaveProjectRequest): Promise<boolean> {
    try {
      await createProject(values)
    } catch (error) {
      if (!(error instanceof ApiError)) {
        setFieldErrors({})
        setFormError(FALLBACK_MESSAGE)
        return false
      }
      const { matched, unmatched } = splitProjectFieldErrors(error.fieldErrors)
      setFieldErrors(matched)
      setFormError([error.message, ...unmatched].join(' '))
      return false
    }
    setFieldErrors({})
    setFormError(null)
    // Re-fetch instead of patching local state, so the list always matches the server.
    await loadProjects()
    return true
  }

  return (
    <section>
      <h1>Projects</h1>

      <h2>New project</h2>
      {formError && <ErrorBanner message={formError} />}
      <ProjectForm onSubmit={handleCreate} fieldErrors={fieldErrors} submitLabel="Create project" />

      <h2>All projects</h2>
      {list.kind === 'loading' && <p>Loading projects…</p>}
      {list.kind === 'error' && <ErrorBanner message={list.message} />}
      {list.kind === 'loaded' &&
        (list.projects.length === 0 ? (
          <EmptyState message="No projects yet. Create your first project." />
        ) : (
          <ul className="project-list">
            {list.projects.map((project) => (
              <li key={project.id}>
                {/* A native link, so it is reachable with Tab and opens with Enter. */}
                <Link className="project-name" to={`/projects/${project.id}`}>
                  {project.name}
                </Link>
                <span className="project-count">
                  {project.taskCount} {project.taskCount === 1 ? 'task' : 'tasks'}
                </span>
              </li>
            ))}
          </ul>
        ))}
    </section>
  )
}
