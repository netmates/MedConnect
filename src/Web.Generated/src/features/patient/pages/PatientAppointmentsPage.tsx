import CancelIcon from '@mui/icons-material/Cancel'
import ChatIcon from '@mui/icons-material/Chat'
import {
  Button,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { appointmentsApi } from '../../../api/appointmentsApi'
import { Page } from '../../../components/Page'
import { AppointmentListFilters } from '../../appointments/AppointmentListFilters'
import { useAppointmentList } from '../../appointments/useAppointmentList'
import { appointmentStatusLabel, formatDateTimeRange } from '../../../lib/format'

export function PatientAppointmentsPage() {
  const list = useAppointmentList(appointmentsApi.myAsPatient)

  async function cancel(id: string) {
    if (!confirm('Отменить запись?')) return
    await list.run(() => appointmentsApi.cancel(id))
  }

  return (
    <Page
      title="Мои записи"
      description="Записи к врачам и переход в чат по приему."
      error={list.error}
    >
      <AppointmentListFilters
        status={list.status}
        period={list.period}
        onStatus={list.selectStatus}
        onPeriod={list.selectPeriod}
      />

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
          {list.items.map((item) => (
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
                      disabled={list.busy}
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
          {list.items.length === 0 && (
            <TableRow>
              <TableCell colSpan={5}>Записей пока нет</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </Page>
  )
}
