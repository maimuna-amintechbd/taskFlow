import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router'
import { messageOf } from '../api/client'
import { deleteTask, listTasks } from '../api/tasks'
import type { TaskDto } from '../api/types'
import ErrorBanner from '../components/ErrorBanner'
import TaskFilters from '../components/TaskFilters'
import { emptyTasksMessage, useTaskFilterParams } from '../components/taskFilterParams'
import TaskTable from '../components/TaskTable'

type ListState =
  | { kind: 'loading' }
  | { kind: 'loaded'; tasks: TaskDto[] }
  | { kind: 'error'; message: string }

/** Every task across all projects, newest first, narrowed by the filters in the URL. */
export default function TasksPage() {
  const [filters, setFilters] = useTaskFilterParams()
  // Plain strings (or undefined) are stable between renders, unlike the filters object,
  // so they are safe effect and callback dependencies.
  const { status, priority } = filters
  const [list, setList] = useState<ListState>({ kind: 'loading' })
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [deletingTask, setDeletingTask] = useState(false)

  // Re-fetch after a change. The old list stays on screen while the request runs;
  // if the re-fetch fails, the error state replaces the table. Uses the current filters.
  const loadTasks = useCallback(async () => {
    try {
      setList({ kind: 'loaded', tasks: await listTasks({ status, priority }) })
    } catch (error) {
      setList({ kind: 'error', message: messageOf(error) })
    }
  }, [status, priority])

  // Runs on mount and again whenever a filter in the URL changes (select, Back, reload).
  useEffect(() => {
    // Ignore a late answer if the page was left, or the filters changed, before it arrived.
    let ignore = false
    // Clear the old rows, so the table never shows a list that doesn't match the selects.
    // Deliberate: this resets the view right before re-syncing with the API.
    // oxlint-disable-next-line react/set-state-in-effect
    setList({ kind: 'loading' })
    listTasks({ status, priority })
      .then((tasks) => {
        if (!ignore) setList({ kind: 'loaded', tasks })
      })
      .catch((error: unknown) => {
        if (!ignore) setList({ kind: 'error', message: messageOf(error) })
      })
    return () => {
      ignore = true
    }
  }, [status, priority])

  async function handleDelete(task: TaskDto) {
    if (deletingTask) return
    if (!window.confirm(`Delete task "${task.title}"?`)) return

    setDeletingTask(true)
    setDeleteError(null)
    try {
      await deleteTask(task.id)
    } catch (error) {
      setDeleteError(messageOf(error))
    } finally {
      // Re-fetch even after a failure: a task already deleted elsewhere then leaves the list.
      await loadTasks()
      setDeletingTask(false)
    }
  }

  return (
    <section>
      <h1>Tasks</h1>
      <p>
        <Link to="/tasks/new">Add task</Link>
      </p>
      <TaskFilters values={filters} onChange={setFilters} disabled={deletingTask} />
      {deleteError && <ErrorBanner message={deleteError} />}
      {list.kind === 'loading' && <p>Loading tasks…</p>}
      {list.kind === 'error' && <ErrorBanner message={list.message} />}
      {list.kind === 'loaded' && (
        <TaskTable
          tasks={list.tasks}
          label="All tasks"
          onDelete={handleDelete}
          deleteDisabled={deletingTask}
          emptyMessage={emptyTasksMessage(filters)}
        />
      )}
    </section>
  )
}
