import AddIcon from '@mui/icons-material/Add'
import CancelIcon from '@mui/icons-material/Cancel'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import SaveIcon from '@mui/icons-material/Save'
import {
  Button,
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
import { slotsApi } from '../../../api/slotsApi'
import { Page } from '../../../components/Page'
import {
  formatApiError,
  formatDateTime,
  fromLocalInputValue,
  parseToLocalInput,
  slotStatusLabel,
  toLocalInputValue,
  todayAtLocal,
} from '../../../lib/format'
import type { ScheduleSlotDto } from '../../../types/slot'

function defaultStart(): string {
  return todayAtLocal(9, 0)
}

function defaultEnd(): string {
  return todayAtLocal(10, 0)
}

export function DoctorSchedulePage() {
  const [items, setItems] = useState<ScheduleSlotDto[]>([])
  const [startTime, setStartTime] = useState(defaultStart)
  const [endTime, setEndTime] = useState(defaultEnd)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await slotsApi.my())
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  function resetForm() {
    setEditingId(null)
    setStartTime(defaultStart())
    setEndTime(defaultEnd())
  }

  function openEdit(slot: ScheduleSlotDto) {
    setEditingId(slot.id)
    setStartTime(toLocalInputValue(slot.startTime))
    setEndTime(toLocalInputValue(slot.endTime))
  }

  function onPasteLocal(
    e: { clipboardData: DataTransfer | null; preventDefault: () => void },
    setter: (value: string) => void,
  ) {
    const text = e.clipboardData?.getData('text')
    if (!text) return
    const parsed = parseToLocalInput(text)
    if (!parsed) return
    e.preventDefault()
    setter(parsed)
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!startTime || !endTime) return
    setBusy(true)
    setError(null)
    const dto = {
      startTime: fromLocalInputValue(startTime),
      endTime: fromLocalInputValue(endTime),
    }
    try {
      if (editingId) await slotsApi.update(editingId, dto)
      else await slotsApi.create(dto)
      resetForm()
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function onDelete(id: string) {
    if (!confirm('Удалить слот?')) return
    setBusy(true)
    setError(null)
    try {
      await slotsApi.remove(id)
      if (editingId === id) resetForm()
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Page
      title="Расписание"
      description="Создание и правка слотов приема. Значение из «Начало» можно скопировать (Ctrl+A, Ctrl+C) и вставить в «Конец» (Ctrl+V)."
      error={error}
    >
      <Stack component="form" spacing={2} onSubmit={(e) => void onSubmit(e)}>
        <Typography variant="h2">
          {editingId ? 'Редактирование слота' : 'Новый слот'}
        </Typography>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <TextField
            label="Начало"
            type="datetime-local"
            required
            fullWidth
            value={startTime}
            onChange={(e) => setStartTime(e.target.value)}
            onPaste={(e) => onPasteLocal(e, setStartTime)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            label="Конец"
            type="datetime-local"
            required
            fullWidth
            value={endTime}
            onChange={(e) => setEndTime(e.target.value)}
            onPaste={(e) => onPasteLocal(e, setEndTime)}
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </Stack>
        <Stack direction="row" spacing={1}>
          <Button
            type="submit"
            variant="contained"
            disabled={busy}
            startIcon={editingId ? <SaveIcon /> : <AddIcon />}
          >
            {editingId ? 'Сохранить' : 'Создать'}
          </Button>
          {editingId && (
            <Button startIcon={<CancelIcon />} onClick={resetForm}>
              Отмена
            </Button>
          )}
        </Stack>
      </Stack>

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Начало</TableCell>
            <TableCell>Конец</TableCell>
            <TableCell>Статус</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((slot) => (
            <TableRow key={slot.id} hover>
              <TableCell>{formatDateTime(slot.startTime)}</TableCell>
              <TableCell>{formatDateTime(slot.endTime)}</TableCell>
              <TableCell>{slotStatusLabel(slot.status)}</TableCell>
              <TableCell align="right">
                {slot.status === 'Available' ? (
                  <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                    <Button
                      size="small"
                      startIcon={<EditIcon />}
                      disabled={busy}
                      onClick={() => openEdit(slot)}
                    >
                      Изменить
                    </Button>
                    <Button
                      size="small"
                      color="error"
                      startIcon={<DeleteIcon />}
                      disabled={busy}
                      onClick={() => void onDelete(slot.id)}
                    >
                      Удалить
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
              <TableCell colSpan={4}>Слотов пока нет</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </Page>
  )
}
