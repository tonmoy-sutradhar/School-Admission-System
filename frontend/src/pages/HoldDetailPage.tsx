import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError, apiFetch } from '../api/client'
import { Countdown } from '../components/Countdown'
import { ErrorBanner } from '../components/ErrorBanner'
import { useStudent } from '../context/StudentContext'
import type { HoldDetail } from '../types/admission'

export function HoldDetailPage() {
  const { holdId } = useParams<{ holdId: string }>()
  const { studentId, isValidStudentId } = useStudent()
  const [hold, setHold] = useState<HoldDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    if (!holdId) return
    try {
      const data = await apiFetch<HoldDetail>(`/holds/${holdId}`)
      setHold(data)
      setError(null)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not load application.')
    }
  }, [holdId])

  useEffect(() => {
    void load()
    const id = window.setInterval(() => void load(), 3000)
    return () => window.clearInterval(id)
  }, [load])

  async function confirm() {
    if (!holdId || !isValidStudentId) {
      setError('Enter your 17-digit birth registration number to pay.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      const data = await apiFetch<{ holdId: string; status: HoldDetail['status'] }>(
        `/holds/${holdId}/confirm`,
        {
          method: 'POST',
          studentId,
        },
      )
      setHold((prev) => (prev ? { ...prev, status: data.status } : prev))
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Payment confirmation failed.')
      await load()
    } finally {
      setBusy(false)
    }
  }

  async function cancel() {
    if (!holdId || !isValidStudentId) {
      setError('Enter your 17-digit birth registration number to cancel.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await apiFetch<void>(`/holds/${holdId}`, {
        method: 'DELETE',
        studentId,
      })
      await load()
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not cancel application.')
      await load()
    } finally {
      setBusy(false)
    }
  }

  const canAct = hold?.status === 'HELD' && new Date(hold.expiresAt).getTime() > Date.now()

  return (
    <section className="page">
      <h1>Application status</h1>
      <ErrorBanner message={error} />
      {!hold ? (
        <p>Loading…</p>
      ) : (
        <div className="hold-detail">
          <p><strong>Hold ID:</strong> {hold.holdId}</p>
          <p><strong>Class ID:</strong> {hold.classId}</p>
          <p><strong>Student:</strong> {hold.studentId}</p>
          <p>
            <strong>Status:</strong>{' '}
            <span className={`status status-${hold.status.toLowerCase()}`}>{hold.status}</span>
          </p>
          {hold.status === 'HELD' && (
            <p>
              <strong>Time left:</strong>{' '}
              <Countdown expiresAt={hold.expiresAt} onExpired={() => void load()} />
            </p>
          )}
          <div className="actions">
            <button type="button" disabled={!canAct || busy} onClick={() => void confirm()}>
              ভর্তি ফি দিন
            </button>
            <button
              type="button"
              className="secondary"
              disabled={!canAct || busy}
              onClick={() => void cancel()}
            >
              বাতিল
            </button>
          </div>
        </div>
      )}
      <p className="muted">
        <Link to="/">Back to classes</Link> · <Link to="/me/holds">My applications</Link>
      </p>
    </section>
  )
}
