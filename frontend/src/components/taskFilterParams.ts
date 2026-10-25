import { useSearchParams } from 'react-router'
import { TASK_PRIORITIES, TASK_STATUSES, type TaskItemStatus, type TaskPriority } from '../api/types'

/** The task list filters a user can choose. undefined means "All". */
export interface TaskFilterValues {
  status?: TaskItemStatus
  priority?: TaskPriority
}

/**
 * Reads and writes the status/priority filters in the URL query string
 * (e.g. /tasks?status=Todo&priority=High), so a reload or a shared link keeps them.
 *
 * Only exact names from TASK_STATUSES / TASK_PRIORITIES count; anything else in the
 * URL ("bogus", "todo") is treated as "All" and never sent to the API.
 * Setting a filter pushes a history entry, so the browser's Back button undoes it.
 */
export function useTaskFilterParams(): [TaskFilterValues, (values: TaskFilterValues) => void] {
  const [searchParams, setSearchParams] = useSearchParams()

  // .find returns the matching array element (typed as the union) or undefined.
  const filters: TaskFilterValues = {
    status: TASK_STATUSES.find((s) => s === searchParams.get('status')),
    priority: TASK_PRIORITIES.find((p) => p === searchParams.get('priority')),
  }

  function setFilters(values: TaskFilterValues) {
    setSearchParams((current) => {
      // Copy, so any other query parameters on the page are kept.
      const next = new URLSearchParams(current)
      // "All" removes the parameter instead of writing an empty value.
      if (values.status === undefined) next.delete('status')
      else next.set('status', values.status)
      if (values.priority === undefined) next.delete('priority')
      else next.set('priority', values.priority)
      return next
    })
  }

  return [filters, setFilters]
}

/** True when at least one filter narrows the list. */
export function hasActiveFilters(values: TaskFilterValues): boolean {
  return values.status !== undefined || values.priority !== undefined
}

/** The empty-list message: a filter-specific one when filters are active. */
export function emptyTasksMessage(values: TaskFilterValues): string {
  return hasActiveFilters(values) ? 'No tasks match these filters.' : 'No tasks yet.'
}
