import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError, FALLBACK_MESSAGE, messageOf, type FieldErrors } from '../api/client'
import { listProjects } from '../api/projects'
import { getTask, updateTask } from '../api/tasks'
import type { ProjectDto, SaveTaskRequest, TaskDto } from '../api/types'
import ErrorBanner from '../components/ErrorBanner'
import TaskForm from '../components/TaskForm'
import { splitTaskFieldErrors } from '../components/taskFormFields'
import NotFoundPage from './NotFoundPage'

const NOT_FOUND_MESSAGE = 'Task not found.'

type LoadState =
  | { kind: 'loading' }
  | { kind: 'loaded'; task: TaskDto; projects: ProjectDto[] }
  | { kind: 'notFound' }
  | { kind: 'error'; message: string }

function stateFromError(error: unknown): LoadState {
  return error instanceof ApiError && error.status === 404
    ? { kind: 'notFound' }
    : { kind: 'error', message: messageOf(error) }
}

/**
 * Route element for /tasks/:id/edit. Only digit-only ids can exist, so anything
 * else (e.g. /tasks/abc/edit) is "not found" without calling the API. The `key`
 * gives each task id a fresh editor, so no state leaks between tasks.
 */
export default function TaskEditPage() {
  const { id = '' } = useParams()
  if (!/^\d+$/.test(id)) return <NotFoundPage message={NOT_FOUND_MESSAGE} />
  return <TaskEditor key={id} taskId={Number(id)} />
}

function TaskEditor({ taskId }: { taskId: number }) {
  const navigate = useNavigate()
  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [formError, setFormError] = useState<string | null>(null)

  useEffect(() => {
    // The task and the Project choices load together; the form needs both.
    // Ignore a late answer if the page was left before the requests finished.
    let ignore = false
    Promise.all([getTask(taskId), listProjects()])
      .then(([task, projects]) => {
        if (!ignore) setState({ kind: 'loaded', task, projects })
      })
      .catch((error: unknown) => {
        if (!ignore) setState(stateFromError(error))
      })
    return () => {
      ignore = true
    }
  }, [taskId])

  async function handleSave(values: SaveTaskRequest): Promise<boolean> {
    let projectId: number
    try {
      projectId = (await updateTask(taskId, values)).projectId
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
    // The task's (possibly new) project page re-fetches, so it lists the saved values.
    navigate(`/projects/${projectId}`)
    return true
  }

  if (state.kind === 'loading') {
    return (
      <section>
        <p>Loading task…</p>
      </section>
    )
  }
  if (state.kind === 'notFound') return <NotFoundPage message={NOT_FOUND_MESSAGE} />
  if (state.kind === 'error') {
    return (
      <section>
        <p>
          <Link to="/tasks">← All tasks</Link>
        </p>
        <ErrorBanner message={state.message} />
      </section>
    )
  }

  const { task, projects } = state
  return (
    <section>
      <p>
        <Link to={`/projects/${task.projectId}`}>← Back to project</Link>
      </p>
      <h1>Edit task</h1>
      {formError && <ErrorBanner message={formError} />}
      {/* Rendered only once the task is loaded, so its values are in the form's first render. */}
      <TaskForm
        projects={projects}
        onSubmit={handleSave}
        fieldErrors={fieldErrors}
        submitLabel="Save"
        initialValues={{
          title: task.title,
          description: task.description ?? '',
          projectId: String(task.projectId),
          status: task.status,
          priority: task.priority,
          dueDate: task.dueDate ?? '',
        }}
      />
    </section>
  )
}
