import { Link } from 'react-router'

interface NotFoundPageProps {
  /** The heading, e.g. "Project not found." Defaults to the generic wording. */
  message?: string
}

export default function NotFoundPage({ message = 'Page not found' }: NotFoundPageProps) {
  return (
    <section>
      <h1>{message}</h1>
      <p>
        There is nothing at this address. <Link to="/">Go to the dashboard</Link>.
      </p>
    </section>
  )
}
