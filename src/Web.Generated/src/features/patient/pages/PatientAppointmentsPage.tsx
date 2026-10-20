import CancelIcon from '@mui/icons-material/Cancel'
import ChatIcon from '@mui/icons-material/Chat'
import {
  Button,
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
import { useCallback, useEffect, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { appointmentsApi } from '../../../api/appointmentsApi'
import { Page } from '../../../components/Page'
import {
  appointmentPeriodRange,
  appointmentStatusLabel,
  formatApiError,
  formatDateTimeRange,
  type AppointmentPeriod,
} from '../../../lib/format'
import type { AppointmentDto } from '../../../types/appointment'

const statusAll = 'all'
const periodAll: AppointmentPeriod = 'all'

export function PatientAppointmentsPage() {
  const [items, setItems] = useState<AppointmentDto[]>([])
  const [status, setStatus] = useState(statusAll)
  const [period, setPeriod] = useState<AppointmentPeriod>(periodAll)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(
        await appointmentsApi.myAsPatient({
          status: status === statusAll ? undefined : status,
          ...appointmentPeriodRange(period),
        }),
      )
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [status, period])

  useEffect(() => {
    void load()
  }, [load])

  async function cancel(id: string) {
    if (!confirm('Отменить запись?')) return
    setBusy(true)
    setError(null)
    try {
      await appointmentsApi.cancel(id)
      await load()
    } catch (e) {
      setError(formatApiError(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Page
      title="Мои записи"
      description="Записи к врачам и переход в чат по приему."
      error={error}
    >
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          select
          label="Статус записи"
          size="small"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
          sx={{ minWidth: 220, maxWidth: 280 }}
        >
          <MenuItem value={statusAll}>Все</MenuItem>
          <MenuItem value="Created">Создана</MenuItem>
          <MenuItem value="Confirmed">Подтверждена</MenuItem>
          <MenuItem value="Cancelled">Отменена</MenuItem>
          <MenuItem value="Completed">Завершена</MenuItem>
        </TextField>
        <TextField
          select
          label="Период"
          size="small"
          value={period}
          onChange={(e) => setPeriod(e.target.value as AppointmentPeriod)}
          sx={{ minWidth: 220, maxWidth: 280 }}
        >
          <MenuItem value={periodAll}>Все</MenuItem>
          <MenuItem value="today">Сегодня</MenuItem>
          <MenuItem value="tomorrow">Завтра</MenuItem>
          <MenuItem value="week">Неделя</MenuItem>
          <MenuItem value="past">Прошедшие</MenuItem>
        </TextField>
      </Stack>

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Врач</TableCell>
            <TableCell>Время</TableCell>
            <TableCell>Статус</TableCell>
            <TableCell>Причина</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((item) => (
            <TableRow key={item.id} hover>
              <TableCell>{item.doctorFullName}</TableCell>
              <TableCell>
                {formatDateTimeRange(item.startTime, item.endTime)}
              </TableCell>
              <TableCell>{appointmentStatusLabel(item.status)}</TableCell>
              <TableCell>{item.reason ?? '—'}</TableCell>
              <TableCell align="right">
                {item.status !== 'Cancelled' && item.status !== 'Completed' ? (
                  <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                    <Button
                      size="small"
                      component={RouterLink}
                      to={`/patient/appointments/${item.id}/chat`}
                      startIcon={<ChatIcon />}
                    >
                      Чат
                    </Button>
                    <Button
                      size="small"
                      color="error"
                      startIcon={<CancelIcon />}
                      disabled={busy}
                      onClick={() => void cancel(item.id)}
                    >
                      Отменить
                    </Button>
                  </Stack>
                ) : (
                  <Typography color="text.secondary">—</Typography>
                )}
              </TableCell>
            </TableRow>
          ))}
          {items.length === 0 && (
            <TableRow>
              <TableCell colSpan={5}>Записей пока нет</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </Page>
  )
}
