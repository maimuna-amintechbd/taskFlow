import { useEffect, useState } from 'react'
import { messageOf } from '../api/client'
import { getDashboard } from '../api/dashboard'
import type { DashboardDto } from '../api/types'
import ErrorBanner from '../components/ErrorBanner'

type DashboardState =
  | { kind: 'loading' }
  | { kind: 'loaded'; totals: DashboardDto }
  | { kind: 'error'; message: string }

export default function DashboardPage() {
  const [dashboard, setDashboard] = useState<DashboardState>({ kind: 'loading' })

  // Runs each time the page opens (or reloads), so the numbers always match saved data.
  useEffect(() => {
    // Ignore a late answer if the page was left before the request finished.
    let ignore = false
    getDashboard()
      .then((totals) => {
        if (!ignore) setDashboard({ kind: 'loaded', totals })
      })
      .catch((error: unknown) => {
        if (!ignore) setDashboard({ kind: 'error', message: messageOf(error) })
      })
    return () => {
      ignore = true
    }
  }, [])

  return (
    <section>
      <h1>Dashboard</h1>
      {dashboard.kind === 'loading' && <p>Loading totals…</p>}
      {dashboard.kind === 'error' && <ErrorBanner message={dashboard.message} />}
      {dashboard.kind === 'loaded' && (
        // A description list pairs each label (dt) with its number (dd) for screen readers.
        <dl className="dashboard-totals">
          <div className="dashboard-total">
            <dt>Total</dt>
            <dd>{dashboard.totals.total}</dd>
          </div>
          <div className="dashboard-total">
            <dt>Completed</dt>
            <dd>{dashboard.totals.completed}</dd>
          </div>
          <div className="dashboard-total">
            <dt>Pending</dt>
            <dd>{dashboard.totals.pending}</dd>
          </div>
        </dl>
      )}
    </section>
  )
}
