import PersonAddIcon from '@mui/icons-material/PersonAdd'
import { Button, Stack, TextField, Typography } from '@mui/material'
import { useState, type FormEvent } from 'react'
import { patientProfileApi } from '../api/patientProfileApi'
import { formatApiError } from '../lib/format'
import type { SignUpPatientDto } from '../types/patient'

const emptyForm: SignUpPatientDto = {
  email: '',
  password: '',
  lastName: '',
  firstName: '',
  middleName: '',
  phone: '',
  dateOfBirth: '',
}

export function PatientSignUpForm({
  onLogin,
  onCancel,
}: {
  onLogin: () => void
  onCancel: () => void
}) {
  const [form, setForm] = useState<SignUpPatientDto>(emptyForm)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    const payload: SignUpPatientDto = {
      email: form.email.trim(),
      password: form.password,
      lastName: form.lastName.trim(),
      firstName: form.firstName.trim(),
      middleName: form.middleName?.trim() || null,
      phone: form.phone?.trim() || null,
      dateOfBirth: form.dateOfBirth?.trim() || null,
    }
    try {
      await patientProfileApi.signUp(payload)
      setDone(true)
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  if (done) {
    return (
      <Stack spacing={2}>
        <Typography>
          Учетная запись создана. Войдите с указанными почтой и паролем.
        </Typography>
        <Button variant="contained" onClick={onLogin}>
          Войти через Keycloak
        </Button>
      </Stack>
    )
  }

  return (
    <Stack component="form" spacing={2} onSubmit={(e) => void onSubmit(e)}>
      {error && <Typography color="error">{error}</Typography>}
      <TextField
        label="Email"
        type="email"
        required
        autoComplete="email"
        value={form.email}
        onChange={(e) => setForm({ ...form, email: e.target.value })}
      />
      <TextField
        label="Пароль"
        type="password"
        required
        autoComplete="new-password"
        value={form.password}
        onChange={(e) => setForm({ ...form, password: e.target.value })}
      />
      <TextField
        label="Фамилия"
        required
        value={form.lastName}
        onChange={(e) => setForm({ ...form, lastName: e.target.value })}
      />
      <TextField
        label="Имя"
        required
        value={form.firstName}
        onChange={(e) => setForm({ ...form, firstName: e.target.value })}
      />
      <TextField
        label="Отчество"
        value={form.middleName ?? ''}
        onChange={(e) => setForm({ ...form, middleName: e.target.value })}
      />
      <TextField
        label="Телефон"
        value={form.phone ?? ''}
        onChange={(e) => setForm({ ...form, phone: e.target.value })}
      />
      <TextField
        label="Дата рождения"
        type="date"
        value={form.dateOfBirth ?? ''}
        onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
        slotProps={{ inputLabel: { shrink: true } }}
      />
      <Button
        type="submit"
        variant="contained"
        disabled={busy}
        startIcon={<PersonAddIcon />}
      >
        Зарегистрироваться
      </Button>
      <Button disabled={busy} onClick={onCancel}>
        Назад ко входу
      </Button>
    </Stack>
  )
}
