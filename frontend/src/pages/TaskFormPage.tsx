import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { ApiError, FALLBACK_MESSAGE, messageOf, type FieldErrors } from '../api/client'
import { listProjects } from '../api/projects'
import { createTask } from '../api/tasks'
import type { ProjectDto, SaveTaskRequest } from '../api/types'
import ErrorBanner from '../components/ErrorBanner'
import TaskForm from '../components/TaskForm'
import { splitTaskFieldErrors } from '../components/taskFormFields'

type ProjectsState =
  | { kind: 'loading' }
  | { kind: 'loaded'; projects: ProjectDto[] }
  | { kind: 'error'; message: string }

/**
 * Route element for /tasks/new?projectId=. Loads the projects for the Project select
 * and preselects ?projectId= when it names one of them. After a save it opens the
 * task's project page, which re-fetches, so the new task shows first in its list.
 */
export default function TaskFormPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [projectsState, setProjectsState] = useState<ProjectsState>({ kind: 'loading' })
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)

  useEffect(() => {
    // Ignore a late answer if the page was left before the request finished.
    let ignore = false
    listProjects()
      .then((projects) => {
        if (!ignore) setProjectsState({ kind: 'loaded', projects })
      })
      .catch((error: unknown) => {
        if (!ignore) setProjectsState({ kind: 'error', message: messageOf(error) })
      })
    return () => {
      ignore = true
    }
  }, [])

  async function handleCreate(values: SaveTaskRequest): Promise<boolean> {
    let projectId: number
    try {
      projectId = (await createTask(values)).projectId
    } catch (error) {
      if (!(error instanceof ApiError)) {
        setFieldErrors({})
        setFormError(FALLBACK_MESSAGE)
        return false
      }
      const { matched, unmatched } = splitTaskFieldErrors(error.fieldErrors)
      setFieldErrors(matched)
      setFormError([error.message, ...unmatched].join(' '))
      return false
    }
    navigate(`/projects/${projectId}`)
    return true
  }

  // Only a digit-only id that matches a loaded project is preselected; anything else
  // (missing, "abc", an unknown id) leaves "Choose a project" selected.
  const queryProjectId = searchParams.get('projectId') ?? ''
  const requestedId = /^\d+$/.test(queryProjectId) ? Number(queryProjectId) : undefined
  const initialProjectId =
    projectsState.kind === 'loaded' && projectsState.projects.some((p) => p.id === requestedId)
      ? requestedId
      : undefined

  return (
    <section>
      <p>
        {initialProjectId === undefined ? (
          <Link to="/tasks">← All tasks</Link>
        ) : (
          <Link to={`/projects/${initialProjectId}`}>← Back to project</Link>
        )}
      </p>
      <h1>New task</h1>
      {projectsState.kind === 'loading' && <p>Loading projects…</p>}
      {projectsState.kind === 'error' && <ErrorBanner message={projectsState.message} />}
      {projectsState.kind === 'loaded' && (
        <>
          {projectsState.projects.length === 0 && (
            <p>
              Every task belongs to a project. <Link to="/projects">Create a project first</Link>.
            </p>
          )}
          {formError && <ErrorBanner message={formError} />}
          {/* Rendered only once the projects are loaded, so the preselection is in its first render. */}
          <TaskForm
            // A new key (a different ?projectId=) gives a fresh form with the new preselection.
            key={queryProjectId}
            projects={projectsState.projects}
            onSubmit={handleCreate}
            fieldErrors={fieldErrors}
            submitLabel="Create task"
            initialProjectId={initialProjectId}
          />
        </>
      )}
    </section>
  )
}
