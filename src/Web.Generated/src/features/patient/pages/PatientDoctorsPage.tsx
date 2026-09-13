import EventAvailableIcon from '@mui/icons-material/EventAvailable'
import ScheduleIcon from '@mui/icons-material/Schedule'
import {
  Button,
  Link,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { appointmentsApi } from '../../../api/appointmentsApi'
import { catalogDoctorsApi } from '../../../api/catalogDoctorsApi'
import { catalogSpecializationsApi } from '../../../api/catalogSpecializationsApi'
import { slotsApi } from '../../../api/slotsApi'
import { Page } from '../../../components/Page'
import {
  formatApiError,
  formatDateTime,
  fullName,
  slotStatusLabel,
  todayDateInput,
} from '../../../lib/format'
import type { DoctorDto } from '../../../types/doctor'
import type { ScheduleSlotDto } from '../../../types/slot'
import type { SpecializationDto } from '../../../types/specialization'

export function PatientDoctorsPage() {
  const [specs, setSpecs] = useState<SpecializationDto[]>([])
  const [doctors, setDoctors] = useState<DoctorDto[]>([])
  const [specId, setSpecId] = useState('')
  const [selectedDoctor, setSelectedDoctor] = useState<DoctorDto | null>(null)
  const [date, setDate] = useState(todayDateInput())
  const [slots, setSlots] = useState<ScheduleSlotDto[]>([])
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const loadCatalog = useCallback(async () => {
    setError(null)
    try {
      const [specializations, list] = await Promise.all([
        catalogSpecializationsApi.list(),
        catalogDoctorsApi.list(specId || undefined),
      ])
      setSpecs(specializations)
      setDoctors(list)
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [specId])

  useEffect(() => {
    void loadCatalog()
  }, [loadCatalog])

  async function loadSlots(doctor: DoctorDto, day: string) {
    setError(null)
    setOk(null)
    try {
      setSlots(await slotsApi.available(doctor.id, day))
    } catch (e) {
      setError(formatApiError(e))
      setSlots([])
    }
  }

  async function openDoctor(doctor: DoctorDto) {
    setSelectedDoctor(doctor)
    setReason('')
    await loadSlots(doctor, date)
  }

  async function onDateChange(value: string) {
    setDate(value)
    if (selectedDoctor) await loadSlots(selectedDoctor, value)
  }

  async function book(slotId: string, e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setOk(null)
    try {
      await appointmentsApi.create({
        slotId,
        reason: reason.trim() || null,
      })
      setOk('Запись создана')
      if (selectedDoctor) await loadSlots(selectedDoctor, date)
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Page
      title="Врачи и запись"
      description={
        <>
          Выберите специалиста и свободный слот. Перед записью нужен{' '}
          <Link component={RouterLink} to="/patient/profile">
            профиль пациента
          </Link>
          .
        </>
      }
      error={error}
      ok={ok}
    >
      <TextField
        select
        label="Специализация"
        size="small"
        value={specId}
        onChange={(e) => {
          setSpecId(e.target.value)
          setSelectedDoctor(null)
          setSlots([])
        }}
        sx={{ maxWidth: 320 }}
      >
        <MenuItem value="">Все</MenuItem>
        {specs.map((s) => (
          <MenuItem key={s.id} value={s.id}>
            {s.name}
          </MenuItem>
        ))}
      </TextField>

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>ФИО</TableCell>
            <TableCell>Стаж</TableCell>
            <TableCell>Специализации</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {doctors.map((doctor) => (
            <TableRow key={doctor.id} hover>
              <TableCell>
                {fullName(doctor.lastName, doctor.firstName, doctor.middleName)}
              </TableCell>
              <TableCell>{doctor.experienceYears}</TableCell>
              <TableCell>{doctor.specializations.join(', ') || '—'}</TableCell>
              <TableCell align="right">
                <Button
                  size="small"
                  startIcon={<ScheduleIcon />}
                  onClick={() => void openDoctor(doctor)}
                >
                  Слоты
                </Button>
              </TableCell>
            </TableRow>
          ))}
          {doctors.length === 0 && (
            <TableRow>
              <TableCell colSpan={4}>Нет активных врачей</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>

      {selectedDoctor && (
        <Stack spacing={2}>
          <Typography variant="h2">
            Слоты:{' '}
            {fullName(
              selectedDoctor.lastName,
              selectedDoctor.firstName,
              selectedDoctor.middleName,
            )}
          </Typography>
          <Typography color="text.secondary">{selectedDoctor.description}</Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label="Дата"
              type="date"
              size="small"
              value={date}
              onChange={(e) => void onDateChange(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              label="Причина визита (необязательно)"
              size="small"
              fullWidth
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
          </Stack>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Время</TableCell>
                <TableCell>Статус</TableCell>
                <TableCell align="right">Действия</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {slots.map((slot) => (
                <TableRow key={slot.id} hover>
                  <TableCell>
                    {formatDateTime(slot.startTime)} — {formatDateTime(slot.endTime)}
                  </TableCell>
                  <TableCell>{slotStatusLabel(slot.status)}</TableCell>
                  <TableCell align="right">
                    <Button
                      size="small"
                      variant="contained"
                      startIcon={<EventAvailableIcon />}
                      disabled={busy || slot.status !== 'Available'}
                      onClick={(e) => void book(slot.id, e)}
                    >
                      Записаться
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {slots.length === 0 && (
                <TableRow>
                  <TableCell colSpan={3}>На этот день свободных слотов нет</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Stack>
      )}
    </Page>
  )
}
