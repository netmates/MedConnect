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
} from '@mui/material'
import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { specializationsApi } from '../../../api/admin/specializationsApi'
import { Page } from '../../../components/Page'
import { formatApiError } from '../../../lib/format'
import type { SpecializationDto } from '../../../types/specialization'

export function SpecializationsPage() {
  const [items, setItems] = useState<SpecializationDto[]>([])
  const [name, setName] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await specializationsApi.list())
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) return

    setBusy(true)
    setError(null)
    try {
      if (editingId) {
        await specializationsApi.update(editingId, { name: trimmed })
      } else {
        await specializationsApi.create({ name: trimmed })
      }
      setName('')
      setEditingId(null)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function onDelete(id: string) {
    if (!confirm('Удалить специализацию?')) return
    setBusy(true)
    setError(null)
    try {
      await specializationsApi.remove(id)
      if (editingId === id) {
        setEditingId(null)
        setName('')
      }
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Page
      title="Специализации"
      description="Справочник для профилей врачей."
      error={error}
    >
      <Stack
        component="form"
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1.5}
        sx={{ alignItems: { sm: 'center' } }}
        onSubmit={(e) => void onSubmit(e)}
      >
        <TextField
          label="Название"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          fullWidth
          size="small"
        />
        <Button
          type="submit"
          variant="contained"
          disabled={busy}
          startIcon={editingId ? <SaveIcon /> : <AddIcon />}
        >
          {editingId ? 'Сохранить' : 'Добавить'}
        </Button>
        {editingId && (
          <Button
            type="button"
            disabled={busy}
            startIcon={<CancelIcon />}
            onClick={() => {
              setEditingId(null)
              setName('')
            }}
          >
            Отмена
          </Button>
        )}
      </Stack>

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Название</TableCell>
            <TableCell>Id</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {items.map((item) => (
            <TableRow key={item.id} hover>
              <TableCell>{item.name}</TableCell>
              <TableCell sx={{ fontFamily: 'ui-monospace, monospace', fontSize: '0.85rem' }}>
                {item.id}
              </TableCell>
              <TableCell align="right">
                <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                  <Button
                    size="small"
                    startIcon={<EditIcon />}
                    disabled={busy}
                    onClick={() => {
                      setEditingId(item.id)
                      setName(item.name)
                    }}
                  >
                    Изменить
                  </Button>
                  <Button
                    size="small"
                    color="error"
                    startIcon={<DeleteIcon />}
                    disabled={busy}
                    onClick={() => void onDelete(item.id)}
                  >
                    Удалить
                  </Button>
                </Stack>
              </TableCell>
            </TableRow>
          ))}
          {items.length === 0 && (
            <TableRow>
              <TableCell colSpan={3}>Пока пусто</TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </Page>
  )
}
