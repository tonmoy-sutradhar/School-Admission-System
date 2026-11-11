import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ApiError, apiFetch } from '../api/client'
import { ErrorBanner } from '../components/ErrorBanner'
import type { ClassSeat } from '../types/admission'

export function CreateClassPage() {
  const [name, setName] = useState('')
  const [totalSeats, setTotalSeats] = useState(40)
  const [error, setError] = useState<string | null>(null)
  const [created, setCreated] = useState<ClassSeat | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setCreated(null)
    setBusy(true)
    try {
      const res = await apiFetch<{ id: string; name: string; totalSeats: number }>('/classes', {
        method: 'POST',
        body: JSON.stringify({ name, totalSeats }),
      })
      setCreated({
        id: res.id,
        name: res.name,
        totalSeats: res.totalSeats,
        heldSeats: 0,
        confirmedSeats: 0,
        availableSeats: res.totalSeats,
      })
      setName('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not create class.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="page">
      <h1>Create class</h1>
      <p className="muted">Each shift is a separate class with its own seat count.</p>
      <ErrorBanner message={error} />
      <form className="form" onSubmit={onSubmit}>
        <label>
          Class name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Class 6 - Morning Shift"
            required
          />
        </label>
        <label>
          Total seats
          <input
            type="number"
            min={1}
            value={totalSeats}
            onChange={(e) => setTotalSeats(Number(e.target.value))}
            required
          />
        </label>
        <button type="submit" disabled={busy || !name.trim()}>
          {busy ? 'Creating…' : 'Create class'}
        </button>
      </form>
      {created && (
        <div className="success-box">
          Created <strong>{created.name}</strong> ({created.totalSeats} seats).{' '}
          <Link to="/">View class list</Link>
        </div>
      )}
    </section>
  )
}
