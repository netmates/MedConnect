import { TextField } from '@mui/material'
import { formatRuPhoneInput } from '../lib/phone'

type PhoneFieldProps = {
  value: string
  onChange: (value: string) => void
}

export function PhoneField({ value, onChange }: PhoneFieldProps) {
  return (
    <TextField
      label="Телефон"
      type="tel"
      placeholder="+7 900 123-45-67"
      value={value}
      onChange={(e) => onChange(formatRuPhoneInput(e.target.value, value))}
      slotProps={{
        inputLabel: { shrink: true },
        htmlInput: { inputMode: 'tel', autoComplete: 'tel' },
      }}
    />
  )
}
