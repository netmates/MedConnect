import { config } from '../config'
import { userManager } from '../auth/userManager'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const user = await userManager.getUser()
  if (!user || user.expired) {
    throw new ApiError(401, 'Требуется вход')
  }

  const headers = new Headers(init.headers)
  headers.set('Authorization', `Bearer ${user.access_token}`)
  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${config.apiUrl}${path}`, {
    ...init,
    headers,
  })

  if (!response.ok) {
    const text = await response.text()
    throw new ApiError(response.status, text || response.statusText)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}
