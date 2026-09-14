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

type TokenProfile = {
  preferred_username?: string
  email?: string
  sub?: string
  family_name?: string
  given_name?: string
}

/** Подпись сессии из OIDC: login (Фамилия Имя). */
export function sessionLabelFromProfile(
  profile: TokenProfile | undefined | null,
  fallback = 'user',
): string {
  const login =
    profile?.preferred_username?.trim() ||
    profile?.email?.trim() ||
    profile?.sub?.trim() ||
    fallback
  const fio = [profile?.family_name, profile?.given_name]
    .map((part) => part?.trim())
    .filter(Boolean)
    .join(' ')
  return fio ? `${login} (${fio})` : login
}

export function formatDateTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function toLocalInputValue(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function fromLocalInputValue(local: string): string {
  const date = new Date(local)
  return date.toISOString()
}

export function todayDateInput(): string {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

/** Сегодняшняя дата с заданным локальным временем (для datetime-local). */
export function todayAtLocal(hours: number, minutes = 0): string {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}T${pad(hours)}:${pad(minutes)}`
}

/** Нормализует вставленный текст к формату datetime-local (yyyy-MM-ddThh:mm). */
export function parseToLocalInput(raw: string): string | null {
  const trimmed = raw.trim()
  const isoLike = trimmed.match(/^(\d{4}-\d{2}-\d{2})[T ](\d{1,2}):(\d{2})/)
  if (isoLike) {
    const [, date, h, m] = isoLike
    return `${date}T${h.padStart(2, '0')}:${m}`
  }

  const ruLike = trimmed.match(/^(\d{1,2})\.(\d{1,2})\.(\d{4})[ T](\d{1,2}):(\d{2})/)
  if (ruLike) {
    const [, d, mo, y, h, m] = ruLike
    return `${y}-${mo.padStart(2, '0')}-${d.padStart(2, '0')}T${h.padStart(2, '0')}:${m}`
  }

  return null
}

export function appointmentStatusLabel(status: string): string {
  switch (status) {
    case 'Created':
      return 'Создана'
    case 'Confirmed':
      return 'Подтверждена'
    case 'Cancelled':
      return 'Отменена'
    case 'Completed':
      return 'Завершена'
    default:
      return status
  }
}

export function slotStatusLabel(status: string): string {
  switch (status) {
    case 'Available':
      return 'Свободен'
    case 'Booked':
      return 'Занят'
    case 'Consumed':
      return 'Использован'
    default:
      return status
  }
}
