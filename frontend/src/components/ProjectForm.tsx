import { useState, type FormEvent } from 'react'
import type { FieldErrors } from '../api/client'
import type { SaveProjectRequest } from '../api/types'
import FieldError from './FieldError'

interface ProjectFormProps {
  /** Called with the typed values; '' becomes null. Resolve true to clear the form. */
  onSubmit: (values: SaveProjectRequest) => Promise<boolean>
  fieldErrors: FieldErrors
  submitLabel: string
}

/**
 * Name and description inputs. The API owns every rule, so there are no HTML
 * constraint attributes; its messages arrive through `fieldErrors`.
 */
export default function ProjectForm({ onSubmit, fieldErrors, submitLabel }: ProjectFormProps) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    try {
      const saved = await onSubmit({
        name: name === '' ? null : name,
        description: description === '' ? null : description,
      })
      // On error the typed values stay so the user can fix them.
      if (saved) {
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
      <button type="submit" disabled={submitting}>
        {submitLabel}
      </button>
    </form>
  )
}
