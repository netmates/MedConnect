import { ApiError } from '../api/http'

export function formatApiError(error: unknown): string {
  if (error instanceof ApiError) {
    if (!error.message) return `Ошибка ${error.status}`
    try {
      const parsed = JSON.parse(error.message) as {
        title?: string
        detail?: string
        errors?: Record<string, string[]>
      }
      if (parsed.errors) {
        return Object.values(parsed.errors).flat().join('; ')
      }
      return parsed.detail ?? parsed.title ?? error.message
    } catch {
      return error.message
    }
  }

  if (error instanceof Error) return error.message
  return 'Неизвестная ошибка'
}

export function fullName(
  lastName: string,
  firstName: string,
  middleName?: string | null,
): string {
  return [lastName, firstName, middleName].filter(Boolean).join(' ')
}
