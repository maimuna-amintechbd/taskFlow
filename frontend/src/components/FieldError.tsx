interface FieldErrorProps {
  /** Matches the input's aria-describedby so screen readers read the message with the field. */
  id: string
  messages?: string[]
}

/** The validation message shown next to one form input. Renders nothing when there is none. */
export default function FieldError({ id, messages }: FieldErrorProps) {
  if (!messages || messages.length === 0) return null
  return (
    <p className="field-error" id={id}>
      {messages.join(' ')}
    </p>
  )
}
