import type { FieldErrors } from '../api/client'
import type { SaveTaskRequest } from '../api/types'

/**
 * The error keys TaskForm shows next to an input. Pages send every other key
 * to the ErrorBanner. Kept in its own file so TaskForm.tsx exports only a component.
 */
export const TASK_FORM_FIELDS = [
  'title',
  'description',
  'projectId',
  'status',
  'priority',
  'dueDate',
] as const satisfies readonly (keyof SaveTaskRequest)[]

/**
 * Splits API field errors into the ones TaskForm shows by an input (`matched`)
 * and the messages of every other key, which belong in the ErrorBanner (`unmatched`).
 */
export function splitTaskFieldErrors(fieldErrors: FieldErrors): {
  matched: FieldErrors
  unmatched: string[]
} {
  const matched: FieldErrors = {}
  const unmatched: string[] = []
  for (const [key, messages] of Object.entries(fieldErrors)) {
    // Widened to string[] so includes() accepts any key the API sent.
    if ((TASK_FORM_FIELDS as readonly string[]).includes(key)) matched[key] = messages
    else unmatched.push(...messages)
  }
  return { matched, unmatched }
}
