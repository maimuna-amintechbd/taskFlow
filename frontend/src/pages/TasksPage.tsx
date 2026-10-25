import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { messageOf } from '../api/client'
import { listTasks } from '../api/tasks'
import type { TaskDto } from '../api/types'
import ErrorBanner from '../components/ErrorBanner'
import TaskTable from '../components/TaskTable'

type ListState =
  | { kind: 'loading' }
  | { kind: 'loaded'; tasks: TaskDto[] }
  | { kind: 'error'; message: string }

/** Every task across all projects, newest first. */
export default function TasksPage() {
  const [list, setList] = useState<ListState>({ kind: 'loading' })

  useEffect(() => {
    // Ignore a late answer if the page was left before the request finished.
    let ignore = false
    listTasks()
      .then((tasks) => {
        if (!ignore) setList({ kind: 'loaded', tasks })
      })
      .catch((error: unknown) => {
        if (!ignore) setList({ kind: 'error', message: messageOf(error) })
      })
    return () => {
      ignore = true
    }
  }, [])

  return (
    <section>
      <h1>Tasks</h1>
      <p>
        <Link to="/tasks/new">Add task</Link>
      </p>
      {list.kind === 'loading' && <p>Loading tasks…</p>}
      {list.kind === 'error' && <ErrorBanner message={list.message} />}
      {list.kind === 'loaded' && <TaskTable tasks={list.tasks} label="All tasks" />}
    </section>
  )
}
