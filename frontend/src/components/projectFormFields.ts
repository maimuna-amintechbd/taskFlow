import type { FieldErrors } from '../api/client'

/**
 * The error keys ProjectForm shows next to an input. Pages send every other key
 * to the ErrorBanner. Kept in its own file so ProjectForm.tsx exports only a component.
 */
export const PROJECT_FORM_FIELDS = ['name', 'description']

/**
 * Splits API field errors into the ones ProjectForm shows by an input (`matched`)
 * and the messages of every other key, which belong in the ErrorBanner (`unmatched`).
 */
export function splitProjectFieldErrors(fieldErrors: FieldErrors): {
  matched: FieldErrors
  unmatched: string[]
} {
  const matched: FieldErrors = {}
  const unmatched: string[] = []
  for (const [key, messages] of Object.entries(fieldErrors)) {
    if (PROJECT_FORM_FIELDS.includes(key)) matched[key] = messages
    else unmatched.push(...messages)
  }
  return { matched, unmatched }
}
