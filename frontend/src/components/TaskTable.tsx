import { Link } from 'react-router'
import type { TaskDto } from '../api/types'
import EmptyState from './EmptyState'

interface TaskTableProps {
  /** Already in display order (the API sends newest first). */
  tasks: TaskDto[]
  /** Names the table for screen readers, e.g. "All tasks". */
  label: string
  /** Called when a row's Delete button is pressed. The page confirms, calls the API and re-fetches. */
  onDelete: (task: TaskDto) => void
  /** Disables every row's Delete button, e.g. while a delete is in flight. */
  deleteDisabled?: boolean
  /** Shown instead of the table when there are no tasks, e.g. when filters match nothing. */
  emptyMessage?: string
}

/** The task list shared by the Tasks page and each project's page. */
export default function TaskTable({
  tasks,
  label,
  onDelete,
  deleteDisabled = false,
  emptyMessage = 'No tasks yet.',
}: TaskTableProps) {
  if (tasks.length === 0) return <EmptyState message={emptyMessage} />

  return (
    // The wrapper scrolls sideways on narrow screens instead of squashing the columns.
    <div className="table-scroll">
      <table className="task-table" aria-label={label}>
        <thead>
          <tr>
            <th scope="col">Title</th>
            <th scope="col">Project</th>
            <th scope="col">Status</th>
            <th scope="col">Priority</th>
            <th scope="col">Due date</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>
        <tbody>
          {tasks.map((task) => (
            <tr key={task.id}>
              <td className="task-title">
                {/* A real link, so Tab and Enter open the edit page too. */}
                <Link to={`/tasks/${task.id}/edit`}>{task.title}</Link>
              </td>
              <td className="task-project">{task.projectName}</td>
              <td>{task.status}</td>
              <td>{task.priority}</td>
              {/* Shown exactly as the API sends it (yyyy-MM-dd); blank when there is none. */}
              <td className="task-due">{task.dueDate ?? ''}</td>
              <td>
                {/* Every row says "Delete", so aria-label names the task for screen readers.
                    It starts with the visible word, so voice control ("click Delete") still works. */}
                <button
                  type="button"
                  className="button-danger"
                  aria-label={`Delete task "${task.title}"`}
                  onClick={() => onDelete(task)}
                  disabled={deleteDisabled}
                >
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
