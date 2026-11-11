import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ApiError, apiFetch } from '../api/client'
import { Countdown } from '../components/Countdown'
import { ErrorBanner } from '../components/ErrorBanner'
import { useStudent } from '../context/StudentContext'
import type { MyHold } from '../types/admission'

export function MyHoldsPage() {
  const { studentId, isValidStudentId } = useStudent()
  const [holds, setHolds] = useState<MyHold[]>([])
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!isValidStudentId) {
      setHolds([])
      return
    }
    try {
      const data = await apiFetch<MyHold[]>('/me/holds', { studentId })
      setHolds(data)
      setError(null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load your applications.')
    }
  }, [isValidStudentId, studentId])

  useEffect(() => {
    void load()
    const id = window.setInterval(() => void load(), 4000)
    return () => window.clearInterval(id)
  }, [load])

  return (
    <section className="page">
      <h1>My applications</h1>
      <p className="muted">Applications for the birth registration number above.</p>
      <ErrorBanner message={error} />
      {!isValidStudentId ? (
        <p>Enter a valid 17-digit number in the top bar to see your applications.</p>
      ) : holds.length === 0 ? (
        <p>
          No applications yet. <Link to="/">Browse classes</Link>.
        </p>
      ) : (
        <ul className="hold-list">
          {holds.map((h) => (
            <li key={h.holdId}>
              <div>
                <h2>{h.className}</h2>
                <p>
                  Status:{' '}
                  <span className={`status status-${h.status.toLowerCase()}`}>{h.status}</span>
                  {h.status === 'HELD' && (
                    <>
                      {' '}
                      · <Countdown expiresAt={h.expiresAt} onExpired={() => void load()} />
                    </>
                  )}
                </p>
              </div>
              <Link to={`/holds/${h.holdId}`}>Open</Link>
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
