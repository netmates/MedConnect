import SaveIcon from '@mui/icons-material/Save'
import { Button, Stack, TextField, Typography } from '@mui/material'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../../../api/http'
import { patientProfileApi } from '../../../api/patientProfileApi'
import { Page } from '../../../components/Page'
import { formatApiError } from '../../../lib/format'
import type { PatientDto, UpdatePatientDto } from '../../../types/patient'

const emptyForm: UpdatePatientDto = {
  lastName: '',
  firstName: '',
  middleName: '',
  phone: '',
  dateOfBirth: '',
}

function toDateInput(value?: string | null): string {
  if (!value) return ''
  return value.slice(0, 10)
}

export function PatientProfilePage() {
  const [profile, setProfile] = useState<PatientDto | null>(null)
  const [needsRegister, setNeedsRegister] = useState(false)
  const [form, setForm] = useState<UpdatePatientDto>(emptyForm)
  const [error, setError] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      const me = await patientProfileApi.me()
      setProfile(me)
      setNeedsRegister(false)
      setForm({
        lastName: me.lastName,
        firstName: me.firstName,
        middleName: me.middleName ?? '',
        phone: me.phone ?? '',
        dateOfBirth: toDateInput(me.dateOfBirth),
      })
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) {
        setNeedsRegister(true)
        setProfile(null)
        setForm(emptyForm)
        return
      }
      setError(formatApiError(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setOk(null)
    const payload: UpdatePatientDto = {
      ...form,
      middleName: form.middleName?.trim() || null,
      phone: form.phone?.trim() || null,
      dateOfBirth: form.dateOfBirth?.trim() || null,
    }
    try {
      const saved = needsRegister
        ? await patientProfileApi.register(payload)
        : await patientProfileApi.updateMe(payload)
      setProfile(saved)
      setNeedsRegister(false)
      setOk(needsRegister ? 'Профиль создан' : 'Сохранено')
      setForm({
        lastName: saved.lastName,
        firstName: saved.firstName,
        middleName: saved.middleName ?? '',
        phone: saved.phone ?? '',
        dateOfBirth: toDateInput(saved.dateOfBirth),
      })
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Page
      title="Профиль"
      description={
        needsRegister
          ? 'Профиля еще нет — заполните данные для регистрации в системе.'
          : 'Редактирование данных пациента.'
      }
      error={error}
      ok={ok}
    >
      <Stack component="form" spacing={2} onSubmit={(e) => void onSubmit(e)}>
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
          startIcon={<SaveIcon />}
          sx={{ alignSelf: 'flex-start' }}
        >
          {needsRegister ? 'Зарегистрировать профиль' : 'Сохранить'}
        </Button>
      </Stack>

      {profile && (
        <Typography color="text.secondary">
          Статус: {profile.isActive ? 'активен' : 'неактивен'} · id: {profile.id}
        </Typography>
      )}
    </Page>
  )
}
