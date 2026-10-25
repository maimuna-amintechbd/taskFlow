import { TASK_PRIORITIES, TASK_STATUSES } from '../api/types'
import type { TaskFilterValues } from './taskFilterParams'

interface TaskFiltersProps {
  /** The current filters; undefined means "All". */
  values: TaskFilterValues
  /** Called with the complete new filter set whenever either select changes. */
  onChange: (values: TaskFilterValues) => void
  /** Disables both selects, e.g. while a delete is in flight and its re-fetch uses the current filters. */
  disabled?: boolean
}

// The "All" option's value. Empty, so it can never collide with a real status or priority.
const ALL = ''

/**
 * Status and priority filters for a task list. Presentational only: it shows the
 * values it is given and reports changes; the page owns the state (in the URL)
 * and the API call. Native selects, so the keyboard works out of the box.
 */
export default function TaskFilters({ values, onChange, disabled = false }: TaskFiltersProps) {
  return (
    <div className="task-filters" role="group" aria-label="Filter tasks">
      <div className="form-field">
        <label htmlFor="filter-status">Status</label>
        <select
          id="filter-status"
          value={values.status ?? ALL}
          disabled={disabled}
          onChange={(e) =>
            onChange({ ...values, status: TASK_STATUSES.find((s) => s === e.target.value) })
          }
        >
          <option value={ALL}>All</option>
          {TASK_STATUSES.map((value) => (
            <option key={value} value={value}>
              {value}
            </option>
          ))}
        </select>
      </div>
      <div className="form-field">
        <label htmlFor="filter-priority">Priority</label>
        <select
          id="filter-priority"
          value={values.priority ?? ALL}
          disabled={disabled}
          onChange={(e) =>
            onChange({ ...values, priority: TASK_PRIORITIES.find((p) => p === e.target.value) })
          }
        >
          <option value={ALL}>All</option>
          {TASK_PRIORITIES.map((value) => (
            <option key={value} value={value}>
              {value}
            </option>
          ))}
        </select>
      </div>
    </div>
  )
}
