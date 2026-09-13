import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import BlockIcon from '@mui/icons-material/Block'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import SaveIcon from '@mui/icons-material/Save'
import {
  Button,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { patientsApi } from '../../../api/admin/patientsApi'
import { Page } from '../../../components/Page'
import { formatApiError, fullName } from '../../../lib/format'
import type { PatientDto, UpdatePatientDto } from '../../../types/patient'

function toDateInput(value?: string | null): string {
  if (!value) return ''
  return value.slice(0, 10)
}

export function PatientDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [patient, setPatient] = useState<PatientDto | null>(null)
  const [form, setForm] = useState<UpdatePatientDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    if (!id) return
    setError(null)
    try {
      const data = await patientsApi.get(id)
      setPatient(data)
      setForm({
        lastName: data.lastName,
        firstName: data.firstName,
        middleName: data.middleName ?? '',
        phone: data.phone ?? '',
        dateOfBirth: toDateInput(data.dateOfBirth),
      })
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!id || !form) return
    setBusy(true)
    setError(null)
    try {
      await patientsApi.update(id, {
        ...form,
        middleName: form.middleName?.trim() || null,
        phone: form.phone?.trim() || null,
        dateOfBirth: form.dateOfBirth?.trim() || null,
      })
      navigate('/admin/patients')
    } catch (err) {
      setError(formatApiError(err))
      setBusy(false)
    }
  }

  async function toggleActive() {
    if (!patient) return
    const action = patient.isActive ? 'деактивировать' : 'активировать'
    if (!confirm(`Точно ${action} пациента?`)) return
    setBusy(true)
    setError(null)
    try {
      if (patient.isActive) await patientsApi.deactivate(patient.id)
      else await patientsApi.activate(patient.id)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  if (!form || !patient) {
    return (
      <Page title="Пациент" error={error}>
        {!error && (
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <CircularProgress size={20} />
            <Typography>Загрузка…</Typography>
          </Stack>
        )}
        <Button
          component={RouterLink}
          to="/admin/patients"
          startIcon={<ArrowBackIcon />}
        >
          К списку
        </Button>
      </Page>
    )
  }

  return (
    <Page
      title={fullName(patient.lastName, patient.firstName, patient.middleName)}
      description={`Статус: ${patient.isActive ? 'активен' : 'неактивен'} · KeycloakId: ${patient.keycloakId}`}
      error={error}
      actions={
        <Button
          component={RouterLink}
          to="/admin/patients"
          startIcon={<ArrowBackIcon />}
        >
          К списку
        </Button>
      }
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
        <Stack direction="row" spacing={1}>
          <Button
            type="submit"
            variant="contained"
            disabled={busy}
            startIcon={<SaveIcon />}
          >
            Сохранить
          </Button>
          <Button
            color={patient.isActive ? 'error' : 'success'}
            disabled={busy}
            startIcon={patient.isActive ? <BlockIcon /> : <CheckCircleIcon />}
            onClick={() => void toggleActive()}
          >
            {patient.isActive ? 'Деактивировать' : 'Активировать'}
          </Button>
        </Stack>
      </Stack>
    </Page>
  )
}
