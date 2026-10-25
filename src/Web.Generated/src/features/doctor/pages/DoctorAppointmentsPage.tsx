import CancelIcon from '@mui/icons-material/Cancel'
import ChatIcon from '@mui/icons-material/Chat'
import CheckIcon from '@mui/icons-material/Check'
import DoneAllIcon from '@mui/icons-material/DoneAll'
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

export function DoctorAppointmentsPage() {
  const list = useAppointmentList(appointmentsApi.myAsDoctor)

  return (
    <Page
      title="Приемы"
      description="Подтверждение, завершение, отмена и чат с пациентом."
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
            <TableCell>Пациент</TableCell>
            <TableCell>Время</TableCell>
            <TableCell>Статус</TableCell>
            <TableCell>Причина</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {list.items.map((item) => (
            <TableRow key={item.id} hover>
              <TableCell>{item.patientFullName}</TableCell>
              <TableCell>
                {formatDateTimeRange(item.startTime, item.endTime)}
              </TableCell>
              <TableCell>{appointmentStatusLabel(item.status)}</TableCell>
              <TableCell>{item.reason ?? '—'}</TableCell>
              <TableCell align="right">
                {item.status !== 'Cancelled' && item.status !== 'Completed' ? (
                  <Stack
                    direction="row"
                    spacing={0.5}
                    useFlexGap
                    sx={{ justifyContent: 'flex-end', flexWrap: 'wrap' }}
                  >
                    <Button
                      size="small"
                      component={RouterLink}
                      to={`/doctor/appointments/${item.id}/chat`}
                      startIcon={<ChatIcon />}
                    >
                      Чат
                    </Button>
                    {item.status === 'Created' && (
                      <Button
                        size="small"
                        variant="contained"
                        startIcon={<CheckIcon />}
                        disabled={list.busy}
                        onClick={() => void list.run(() => appointmentsApi.confirm(item.id))}
                      >
                        Подтвердить
                      </Button>
                    )}
                    {item.status === 'Confirmed' && (
                      <Button
                        size="small"
                        variant="contained"
                        startIcon={<DoneAllIcon />}
                        disabled={list.busy}
                        onClick={() => void list.run(() => appointmentsApi.complete(item.id))}
                      >
                        Завершить
                      </Button>
                    )}
                    <Button
                      size="small"
                      color="error"
                      startIcon={<CancelIcon />}
                      disabled={list.busy}
                      onClick={() => {
                        if (!confirm('Отменить прием?')) return
                        void list.run(() => appointmentsApi.cancel(item.id))
                      }}
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
              <TableCell colSpan={5}>Приемов пока нет</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </Page>
  )
}
