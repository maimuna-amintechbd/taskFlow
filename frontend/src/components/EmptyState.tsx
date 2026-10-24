interface EmptyStateProps {
  message: string
}

/** Shown in place of a list that has no items yet. */
export default function EmptyState({ message }: EmptyStateProps) {
  return <p className="empty-state">{message}</p>
}
