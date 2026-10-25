import { Link } from 'react-router'
import type { TaskDto } from '../api/types'
import EmptyState from './EmptyState'

interface TaskTableProps {
  /** Already in display order (the API sends newest first). */
  tasks: TaskDto[]
  /** Names the table for screen readers, e.g. "All tasks". */
  label: string
}

/** The task list shared by the Tasks page and each project's page. */
export default function TaskTable({ tasks, label }: TaskTableProps) {
  if (tasks.length === 0) return <EmptyState message="No tasks yet." />

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
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
