const nationalGroups = [3, 3, 2, 2] as const
const groupSeparators = [' ', ' ', '-', '-'] as const

/**
 * Маска российского номера: +7 XXX XXX-XX-XX.
 * Ведущая 8 заменяется на 7. Пустой ввод и стирание кода страны дают пустую строку.
 */
export function formatRuPhoneInput(raw: string, previous = ''): string {
  const digitsOnly = raw.replace(/\D/g, '')
  if (!digitsOnly) return ''

  let digits = digitsOnly
  if (digits.startsWith('8')) digits = `7${digits.slice(1)}`
  if (!digits.startsWith('7')) digits = `7${digits}`
  digits = digits.slice(0, 11)

  const national = digits.slice(1)
  if (!national && raw.length < previous.length) return ''
  if (!national) return '+7'

  let result = '+7'
  let offset = 0
  for (let i = 0; i < nationalGroups.length && offset < national.length; i++) {
    const part = national.slice(offset, offset + nationalGroups[i])
    result += groupSeparators[i] + part
    offset += part.length
  }
  return result
}

/** Пустой ввод и одинокий код страны не отправляются. */
export function phoneOrNull(value: string | null | undefined): string | null {
  const formatted = formatRuPhoneInput(value ?? '')
  if (formatted.replace(/\D/g, '').length <= 1) return null
  return formatted
}
