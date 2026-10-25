import { useState, type FormEvent } from 'react'
import type { FieldErrors } from '../api/client'
import {
  TASK_PRIORITIES,
  TASK_STATUSES,
  type ProjectDto,
  type SaveTaskRequest,
  type TaskItemStatus,
  type TaskPriority,
} from '../api/types'
import FieldError from './FieldError'

interface TaskFormProps {
  /** The choices for the Project select. The page loads them; this component never calls the API. */
  projects: ProjectDto[]
  /** Called with the typed values; '' becomes null and projectId is a number. Resolve true when saved. */
  onSubmit: (values: SaveTaskRequest) => Promise<boolean>
  fieldErrors: FieldErrors
  submitLabel: string
  /** The project selected at first (e.g. from ?projectId=). Without it, no project is chosen. */
  initialProjectId?: number
}

/**
 * Title, description, project, status, priority and due date inputs. The API owns
 * every rule, so there are no HTML constraint attributes (no required, no min on the
 * date); its messages arrive through `fieldErrors`. Story 3.3 adds edit mode.
 */
export default function TaskForm({
  projects,
  onSubmit,
  fieldErrors,
  submitLabel,
  initialProjectId,
}: TaskFormProps) {
  // Every input holds a string while editing, exactly like the DOM does.
  // useState reads its argument only on the first render.
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [projectId, setProjectId] = useState(initialProjectId === undefined ? '' : String(initialProjectId))
  const [status, setStatus] = useState<TaskItemStatus>('Todo')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  // The date input's value is already 'yyyy-MM-dd' (or ''), so it is sent as is.
  const [dueDate, setDueDate] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    try {
      // On error the typed values stay so the user can fix them; on success the page navigates away.
      await onSubmit({
        title: title === '' ? null : title,
        description: description === '' ? null : description,
        projectId: projectId === '' ? null : Number(projectId),
        status,
        priority,
        dueDate: dueDate === '' ? null : dueDate,
      })
    } finally {
      setSubmitting(false)
    }
  }

  // Wires an input to its FieldError: aria-invalid and aria-describedby only when there is a message.
  function errorProps(key: string, errorId: string) {
    return fieldErrors[key]
      ? { 'aria-invalid': true, 'aria-describedby': errorId }
      : {}
  }

  return (
    <form className="task-form" onSubmit={handleSubmit} noValidate>
      <div className="form-field">
        <label htmlFor="task-title">Title</label>
        <input
          id="task-title"
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          {...errorProps('title', 'task-title-error')}
        />
        <FieldError id="task-title-error" messages={fieldErrors.title} />
      </div>
      <div className="form-field">
        <label htmlFor="task-description">Description (optional)</label>
        <textarea
          id="task-description"
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          {...errorProps('description', 'task-description-error')}
        />
        <FieldError id="task-description-error" messages={fieldErrors.description} />
      </div>
      <div className="form-field">
        <label htmlFor="task-project">Project</label>
        <select
          id="task-project"
          value={projectId}
          onChange={(e) => setProjectId(e.target.value)}
          {...errorProps('projectId', 'task-project-error')}
        >
          <option value="">Choose a project</option>
          {projects.map((project) => (
            <option key={project.id} value={String(project.id)}>
              {project.name}
            </option>
          ))}
        </select>
        <FieldError id="task-project-error" messages={fieldErrors.projectId} />
      </div>
      <div className="form-field">
        <label htmlFor="task-status">Status</label>
        <select
          id="task-status"
          value={status}
          onChange={(e) => setStatus(e.target.value as TaskItemStatus)}
          {...errorProps('status', 'task-status-error')}
        >
          {TASK_STATUSES.map((value) => (
            <option key={value} value={value}>
              {value}
            </option>
          ))}
        </select>
        <FieldError id="task-status-error" messages={fieldErrors.status} />
      </div>
      <div className="form-field">
        <label htmlFor="task-priority">Priority</label>
        <select
          id="task-priority"
          value={priority}
          onChange={(e) => setPriority(e.target.value as TaskPriority)}
          {...errorProps('priority', 'task-priority-error')}
        >
          {TASK_PRIORITIES.map((value) => (
            <option key={value} value={value}>
              {value}
            </option>
          ))}
        </select>
        <FieldError id="task-priority-error" messages={fieldErrors.priority} />
      </div>
      <div className="form-field">
        <label htmlFor="task-due-date">Due date (optional)</label>
        <input
          id="task-due-date"
          type="date"
          value={dueDate}
          onChange={(e) => setDueDate(e.target.value)}
          {...errorProps('dueDate', 'task-due-date-error')}
        />
        <FieldError id="task-due-date-error" messages={fieldErrors.dueDate} />
      </div>
      <div className="button-row">
        <button type="submit" disabled={submitting}>
          {submitLabel}
        </button>
      </div>
    </form>
  )
}
