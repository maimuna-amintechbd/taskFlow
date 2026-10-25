import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, FALLBACK_MESSAGE, messageOf, type FieldErrors } from '../api/client'
import { deleteProject, getProject, updateProject } from '../api/projects'
import { deleteTask, listTasks } from '../api/tasks'
import type { ProjectDto, SaveProjectRequest, TaskDto } from '../api/types'
import ErrorBanner from '../components/ErrorBanner'
import ProjectForm from '../components/ProjectForm'
import { splitProjectFieldErrors } from '../components/projectFormFields'
import TaskFilters from '../components/TaskFilters'
import { emptyTasksMessage, useTaskFilterParams } from '../components/taskFilterParams'
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
  const [filters, setFilters] = useTaskFilterParams()
  // Plain strings (or undefined), so they are stable effect and callback dependencies.
  const { status, priority } = filters
  const [state, setState] = useState<ProjectState>({ kind: 'loading' })
  const [editing, setEditing] = useState(false)
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [tasksState, setTasksState] = useState<TasksState>({ kind: 'loading' })
  const [taskDeleteError, setTaskDeleteError] = useState<string | null>(null)
  const [deletingTask, setDeletingTask] = useState(false)

  const loadProject = useCallback(async () => {
    try {
      setState({ kind: 'loaded', project: await getProject(projectId) })
    } catch (error) {
      setState(stateFromError(error))
    }
  }, [projectId])

  const loadTasks = useCallback(async () => {
    try {
      setTasksState({ kind: 'loaded', tasks: await listTasks({ projectId, status, priority }) })
    } catch (error) {
      setTasksState({ kind: 'error', message: messageOf(error) })
    }
  }, [projectId, status, priority])

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
  // They re-load whenever a filter in the URL changes (select, Back, reload).
  useEffect(() => {
    let ignore = false
    // Clear the old rows, so the table never shows a list that doesn't match the selects.
    // Deliberate: this resets the view right before re-syncing with the API.
    // oxlint-disable-next-line react/set-state-in-effect
    setTasksState({ kind: 'loading' })
    listTasks({ projectId, status, priority })
      .then((tasks) => {
        if (!ignore) setTasksState({ kind: 'loaded', tasks })
      })
      .catch((error: unknown) => {
        if (!ignore) setTasksState({ kind: 'error', message: messageOf(error) })
      })
    return () => {
      ignore = true
    }
  }, [projectId, status, priority])

  function startEditing() {
    setFieldErrors({})
    setFormError(null)
    setDeleteError(null)
    setTaskDeleteError(null)
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
    setTaskDeleteError(null)
    try {
      await deleteProject(projectId)
    } catch (error) {
      setDeleteError(messageOf(error))
      setDeleting(false)
      return
    }
    navigate('/projects')
  }

  async function handleDeleteTask(task: TaskDto) {
    if (deletingTask || deleting) return
    if (!window.confirm(`Delete task "${task.title}"?`)) return

    setDeletingTask(true)
    setTaskDeleteError(null)
    try {
      await deleteTask(task.id)
    } catch (error) {
      setTaskDeleteError(messageOf(error))
    } finally {
      // Re-fetch even after a failure, so a task already deleted elsewhere leaves the list.
      // The project is re-fetched too, so its task count matches the list.
      await Promise.all([loadProject(), loadTasks()])
      setDeletingTask(false)
    }
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
              disabled={deleting || deletingTask}
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
      <TaskFilters values={filters} onChange={setFilters} disabled={deletingTask || deleting} />
      {taskDeleteError && <ErrorBanner message={taskDeleteError} />}
      {tasksState.kind === 'loading' && <p>Loading tasks…</p>}
      {tasksState.kind === 'error' && <ErrorBanner message={tasksState.message} />}
      {tasksState.kind === 'loaded' && (
        <TaskTable
          tasks={tasksState.tasks}
          label={`Tasks in ${project.name}`}
          onDelete={handleDeleteTask}
          deleteDisabled={deletingTask || deleting}
          emptyMessage={emptyTasksMessage(filters)}
        />
      )}
    </section>
  )
}
