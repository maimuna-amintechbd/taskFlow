import { useState, type FormEvent } from 'react'
import type { FieldErrors } from '../api/client'
import type { SaveProjectRequest } from '../api/types'
import FieldError from './FieldError'

interface ProjectFormProps {
  /** Called with the typed values; '' becomes null. Resolve true when saved (clears the form in create mode). */
  onSubmit: (values: SaveProjectRequest) => Promise<boolean>
  fieldErrors: FieldErrors
  submitLabel: string
  /** Seeds the inputs when editing. Without it the form is in create mode: empty, and cleared after a save. */
  initialValues?: { name: string; description: string }
  /** When given, a Cancel button is shown that calls it. */
  onCancel?: () => void
}

/**
 * Name and description inputs. The API owns every rule, so there are no HTML
 * constraint attributes; its messages arrive through `fieldErrors`.
 */
export default function ProjectForm({
  onSubmit,
  fieldErrors,
  submitLabel,
  initialValues,
  onCancel,
}: ProjectFormProps) {
  // useState reads its argument only on the first render, so later prop changes
  // don't overwrite what the user is typing.
  const [name, setName] = useState(initialValues?.name ?? '')
  const [description, setDescription] = useState(initialValues?.description ?? '')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    try {
      const saved = await onSubmit({
        name: name === '' ? null : name,
        description: description === '' ? null : description,
      })
      // On error the typed values stay so the user can fix them. A successful create
      // clears the form; in edit mode the page decides what happens next.
      if (saved && !initialValues) {
        setName('')
        setDescription('')
      }
    } finally {
      setSubmitting(false)
    }
  }

  const nameError = fieldErrors.name
  const descriptionError = fieldErrors.description

  return (
    <form className="project-form" onSubmit={handleSubmit} noValidate>
      <div className="form-field">
        <label htmlFor="project-name">Name</label>
        <input
          id="project-name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          aria-invalid={nameError ? true : undefined}
          aria-describedby={nameError ? 'project-name-error' : undefined}
        />
        <FieldError id="project-name-error" messages={nameError} />
      </div>
      <div className="form-field">
        <label htmlFor="project-description">Description (optional)</label>
        <textarea
          id="project-description"
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          aria-invalid={descriptionError ? true : undefined}
          aria-describedby={descriptionError ? 'project-description-error' : undefined}
        />
        <FieldError id="project-description-error" messages={descriptionError} />
      </div>
      <div className="button-row">
        <button type="submit" disabled={submitting}>
          {submitLabel}
        </button>
        {onCancel && (
          <button type="button" className="button-secondary" onClick={onCancel} disabled={submitting}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
