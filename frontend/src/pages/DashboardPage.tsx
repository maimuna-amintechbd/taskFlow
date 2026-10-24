import { useEffect, useState } from 'react'
import { ApiError } from '../api/client'
import { getHealth } from '../api/health'
import ErrorBanner from '../components/ErrorBanner'

type HealthState =
  | { kind: 'loading' }
  | { kind: 'connected' }
  | { kind: 'error'; message: string }

export default function DashboardPage() {
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })

  useEffect(() => {
    // Ignore a late answer if the page was left before the request finished.
    let ignore = false
    getHealth()
      .then(() => {
        if (!ignore) setHealth({ kind: 'connected' })
      })
      .catch((error: unknown) => {
        if (ignore) return
        const message =
          error instanceof ApiError ? error.message : 'Something went wrong. Please try again.'
        setHealth({ kind: 'error', message })
      })
    return () => {
      ignore = true
    }
  }, [])

  return (
    <section>
      <h1>Dashboard</h1>
      {health.kind === 'loading' && <p>Checking the API…</p>}
      {health.kind === 'connected' && <p>API connected</p>}
      {health.kind === 'error' && <ErrorBanner message={health.message} />}
    </section>
  )
}
