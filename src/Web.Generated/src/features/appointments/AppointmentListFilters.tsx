import { MenuItem, Stack, TextField } from '@mui/material'
import type { AppointmentPeriod } from '../../lib/format'

const statusAll = 'all'
const periodAll: AppointmentPeriod = 'all'

export function AppointmentListFilters({
  status,
  period,
  onStatus,
  onPeriod,
}: {
  status: string
  period: AppointmentPeriod
  onStatus: (status: string) => void
  onPeriod: (period: AppointmentPeriod) => void
}) {
  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
      <TextField
        select
        label="Статус записи"
        size="small"
        value={status}
        onChange={(e) => onStatus(e.target.value)}
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
        onChange={(e) => onPeriod(e.target.value as AppointmentPeriod)}
        sx={{ minWidth: 220, maxWidth: 280 }}
      >
        <MenuItem value={periodAll}>Все</MenuItem>
        <MenuItem value="today">Сегодня</MenuItem>
        <MenuItem value="tomorrow">Завтра</MenuItem>
        <MenuItem value="week">Неделя</MenuItem>
        <MenuItem value="past">Прошедшие</MenuItem>
      </TextField>
    </Stack>
  )
}
