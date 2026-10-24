import { Link } from 'react-router'

export default function NotFoundPage() {
  return (
    <section>
      <h1>Page not found</h1>
      <p>
        There is nothing at this address. <Link to="/">Go to the dashboard</Link>.
      </p>
    </section>
  )
}
