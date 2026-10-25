import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, FALLBACK_MESSAGE, messageOf, type FieldErrors } from '../api/client'
import { deleteProject, getProject, updateProject } from '../api/projects'
import { listTasks } from '../api/tasks'
import type { ProjectDto, SaveProjectRequest, TaskDto } from '../api/types'
import ErrorBanner from '../components/ErrorBanner'
import ProjectForm from '../components/ProjectForm'
import { splitProjectFieldErrors } from '../components/projectFormFields'
import TaskTable from '../components/TaskTable'
import NotFoundPage from './NotFoundPage'

const NOT_FOUND_MESSAGE = 'Project not found.'

type ProjectState =
  | { kind: 'loading' }
  | { kind: 'loaded'; project: ProjectDto }
  | { kind: 'notFound' }
  | { kind: 'error'; message: string }

type TasksState =
  | { kind: 'loading' }
  | { kind: 'loaded'; tasks: TaskDto[] }
  | { kind: 'error'; message: string }

function stateFromError(error: unknown): ProjectState {
  return error instanceof ApiError && error.status === 404
    ? { kind: 'notFound' }
    : { kind: 'error', message: messageOf(error) }
}

function taskCountLabel(count: number): string {
  return `${count} ${count === 1 ? 'task' : 'tasks'}`
}

/**
 * Route element for /projects/:id. Only digit-only ids can exist, so anything
 * else (e.g. /projects/abc) is "not found" without calling the API. The `key`
 * gives each project id a fresh ProjectDetails, so no state leaks between projects.
 */
export default function ProjectPage() {
  const { id = '' } = useParams()
  if (!/^\d+$/.test(id)) return <NotFoundPage message={NOT_FOUND_MESSAGE} />
  return <ProjectDetails key={id} projectId={Number(id)} />
}

function ProjectDetails({ projectId }: { projectId: number }) {
  const navigate = useNavigate()
  const [state, setState] = useState<ProjectState>({ kind: 'loading' })
  const [editing, setEditing] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [tasksState, setTasksState] = useState<TasksState>({ kind: 'loading' })

  const loadProject = useCallback(async () => {
    try {
      setState({ kind: 'loaded', project: await getProject(projectId) })
    } catch (error) {
      setState(stateFromError(error))
    }
  }, [projectId])

  const loadTasks = useCallback(async () => {
    try {
      setTasksState({ kind: 'loaded', tasks: await listTasks({ projectId }) })
    } catch (error) {
      setTasksState({ kind: 'error', message: messageOf(error) })
    }
  }, [projectId])

  useEffect(() => {
    // Ignore a late answer if the page was left before the request finished.
    let ignore = false
    getProject(projectId)
      .then((project) => {
        if (!ignore) setState({ kind: 'loaded', project })
      })
      .catch((error: unknown) => {
        if (!ignore) setState(stateFromError(error))
      })
    return () => {
      ignore = true
    }
  }, [projectId])

  // This project's tasks load on their own, so a failure here leaves the details usable.
  useEffect(() => {
    let ignore = false
    listTasks({ projectId })
      .then((tasks) => {
        if (!ignore) setTasksState({ kind: 'loaded', tasks })
      })
      .catch((error: unknown) => {
        if (!ignore) setTasksState({ kind: 'error', message: messageOf(error) })
      })
    return () => {
      ignore = true
    }
  }, [projectId])

  function startEditing() {
    setFieldErrors({})
    setFormError(null)
    setDeleteError(null)
    setEditing(true)
  }

  function cancelEditing() {
    setFieldErrors({})
    setFormError(null)
    setEditing(false)
  }

  async function handleSave(values: SaveProjectRequest): Promise<boolean> {
    try {
      await updateProject(projectId, values)
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
    // Re-fetch instead of using the PUT response, so the page always shows the server's
    // data. Leave edit mode afterwards, so the details never flash the old values.
    // The tasks are re-fetched too: their rows show the (possibly renamed) project.
    await Promise.all([loadProject(), loadTasks()])
    setEditing(false)
    return true
  }

  async function handleDelete(project: ProjectDto) {
    // taskCount comes from the latest fetch of this project.
    const confirmed = window.confirm(
      `Delete project "${project.name}" and its ${taskCountLabel(project.taskCount)}?`,
    )
    if (!confirmed) return

    setDeleting(true)
    setDeleteError(null)
    try {
      await deleteProject(projectId)
    } catch (error) {
      setDeleteError(messageOf(error))
      setDeleting(false)
      return
    }
    navigate('/projects')
  }

  if (state.kind === 'loading') {
    return (
      <section>
        <p>Loading project…</p>
      </section>
    )
  }
  if (state.kind === 'notFound') return <NotFoundPage message={NOT_FOUND_MESSAGE} />
  if (state.kind === 'error') {
    return (
      <section>
        <p>
          <Link to="/projects">← All projects</Link>
        </p>
        <ErrorBanner message={state.message} />
      </section>
    )
  }

  const { project } = state
  return (
    <section>
      <p>
        <Link to="/projects">← All projects</Link>
      </p>
      <h1 className="project-title">{project.name}</h1>

      {editing ? (
        <>
          <h2>Edit project</h2>
          {formError && <ErrorBanner message={formError} />}
          <ProjectForm
            onSubmit={handleSave}
            fieldErrors={fieldErrors}
            submitLabel="Save"
            initialValues={{ name: project.name, description: project.description ?? '' }}
            onCancel={cancelEditing}
          />
        </>
      ) : (
        <div className="project-details">
          {project.description !== null && (
            <p className="project-description">{project.description}</p>
          )}
          <p className="project-count">{taskCountLabel(project.taskCount)}</p>
          {deleteError && <ErrorBanner message={deleteError} />}
          <div className="button-row">
            <button
              type="button"
              className="button-secondary"
              onClick={startEditing}
              disabled={deleting}
            >
              Edit
            </button>
            <button
              type="button"
              className="button-danger"
              onClick={() => handleDelete(project)}
              disabled={deleting}
            >
              Delete
            </button>
          </div>
        </div>
      )}

      <h2>Tasks</h2>
      <p>
        <Link to={`/tasks/new?projectId=${projectId}`}>Add task</Link>
      </p>
      {tasksState.kind === 'loading' && <p>Loading tasks…</p>}
      {tasksState.kind === 'error' && <ErrorBanner message={tasksState.message} />}
      {tasksState.kind === 'loaded' && (
        <TaskTable tasks={tasksState.tasks} label={`Tasks in ${project.name}`} />
      )}
    </section>
  )
}
