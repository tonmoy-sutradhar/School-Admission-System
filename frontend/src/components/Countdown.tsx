import { useEffect, useState } from 'react'

function remainingMs(expiresAt: string) {
  return new Date(expiresAt).getTime() - Date.now()
}

function format(ms: number) {
  if (ms <= 0) return '00:00'
  const totalSec = Math.ceil(ms / 1000)
  const m = Math.floor(totalSec / 60)
  const s = totalSec % 60
  return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
}

export function Countdown({
  expiresAt,
  onExpired,
}: {
  expiresAt: string
  onExpired?: () => void
}) {
  const [ms, setMs] = useState(() => remainingMs(expiresAt))

  useEffect(() => {
    setMs(remainingMs(expiresAt))
    const id = window.setInterval(() => {
      const next = remainingMs(expiresAt)
      setMs(next)
      if (next <= 0) {
        window.clearInterval(id)
        onExpired?.()
      }
    }, 250)
    return () => window.clearInterval(id)
  }, [expiresAt, onExpired])

  const expired = ms <= 0
  return (
    <span className={expired ? 'countdown expired' : 'countdown'}>
      {expired ? 'EXPIRED' : format(ms)}
    </span>
  )
}
