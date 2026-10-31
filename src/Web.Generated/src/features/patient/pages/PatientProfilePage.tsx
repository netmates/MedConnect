import SaveIcon from '@mui/icons-material/Save'
import { Button, Stack, TextField, Typography } from '@mui/material'
import { useEffect, useState, type FormEvent } from 'react'
import { patientProfileApi } from '../../../api/patientProfileApi'
import { Page } from '../../../components/Page'
import { PhoneField } from '../../../components/PhoneField'
import { formatApiError } from '../../../lib/format'
import { formatRuPhoneInput, phoneOrNull } from '../../../lib/phone'
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
  const [form, setForm] = useState<UpdatePatientDto>(emptyForm)
  const [error, setError] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let cancelled = false
    void patientProfileApi
      .me()
      .then((me) => {
        if (cancelled) return
        setProfile(me)
        setError(null)
        setForm({
          lastName: me.lastName,
          firstName: me.firstName,
          middleName: me.middleName ?? '',
          phone: formatRuPhoneInput(me.phone ?? ''),
          dateOfBirth: toDateInput(me.dateOfBirth),
        })
      })
      .catch((e: unknown) => {
        if (!cancelled) setError(formatApiError(e))
      })
    return () => {
      cancelled = true
    }
  }, [])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setOk(null)
    const payload: UpdatePatientDto = {
      ...form,
      middleName: form.middleName?.trim() || null,
      phone: phoneOrNull(form.phone),
      dateOfBirth: form.dateOfBirth?.trim() || null,
    }
    try {
      const saved = await patientProfileApi.updateMe(payload)
      setProfile(saved)
      setOk('Сохранено')
      setForm({
        lastName: saved.lastName,
        firstName: saved.firstName,
        middleName: saved.middleName ?? '',
        phone: formatRuPhoneInput(saved.phone ?? ''),
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
      description="Редактирование данных пациента."
      error={error}
      ok={ok}
    >
      {profile && (
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
          <PhoneField
            value={form.phone ?? ''}
            onChange={(phone) => setForm({ ...form, phone })}
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
            Сохранить
          </Button>
          <Typography color="text.secondary">
            Статус: {profile.isActive ? 'активен' : 'неактивен'} · id: {profile.id}
          </Typography>
        </Stack>
      )}
    </Page>
  )
}
