import type { ApiErrorBody } from '../types/admission'

const API_BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '')
  || 'http://localhost:8080'

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function parseError(res: Response): Promise<string> {
  try {
    const body = (await res.json()) as ApiErrorBody
    if (body?.message) return body.message
  } catch {
    /* ignore */
  }
  return `Request failed (${res.status})`
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit & { studentId?: string | null } = {},
): Promise<T> {
  const { studentId, headers, ...rest } = options
  const finalHeaders = new Headers(headers)
  finalHeaders.set('Accept', 'application/json')
  if (rest.body && !finalHeaders.has('Content-Type')) {
    finalHeaders.set('Content-Type', 'application/json')
  }
  if (studentId) {
    finalHeaders.set('X-Student-Id', studentId)
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...rest,
    headers: finalHeaders,
  })

  if (res.status === 204) {
    return undefined as T
  }

  if (!res.ok) {
    throw new ApiError(res.status, await parseError(res))
  }

  return (await res.json()) as T
}

export { API_BASE }
