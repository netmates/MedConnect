import { useCallback, useEffect, useState, type ClipboardEvent, type FormEvent } from 'react'
import { slotsApi } from '../../../api/slotsApi'
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
    e: ClipboardEvent<HTMLInputElement>,
    setter: (value: string) => void,
  ) {
    const parsed = parseToLocalInput(e.clipboardData.getData('text'))
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
    <section className="panel panel-wide">
      <h1>Расписание</h1>
      <p className="lead">
        Создание и правка слотов приема. Значение из «Начало» можно скопировать
        (Ctrl+A, Ctrl+C) и вставить в «Конец» (Ctrl+V).
      </p>

      {error && <p className="error-banner">{error}</p>}

      <form className="form-grid" onSubmit={(e) => void onSubmit(e)}>
        <h2>{editingId ? 'Редактирование слота' : 'Новый слот'}</h2>
        <label>
          Начало
          <input
            className="input"
            type="datetime-local"
            required
            value={startTime}
            onChange={(e) => setStartTime(e.target.value)}
            onPaste={(e) => onPasteLocal(e, setStartTime)}
          />
        </label>
        <label>
          Конец
          <input
            className="input"
            type="datetime-local"
            required
            value={endTime}
            onChange={(e) => setEndTime(e.target.value)}
            onPaste={(e) => onPasteLocal(e, setEndTime)}
          />
        </label>
        <div className="btn-row">
          <button className="btn btn-primary" type="submit" disabled={busy}>
            {editingId ? 'Сохранить' : 'Создать'}
          </button>
          {editingId && (
            <button className="btn btn-ghost-dark" type="button" onClick={resetForm}>
              Отмена
            </button>
          )}
        </div>
      </form>

      <table className="table">
        <thead>
          <tr>
            <th>Начало</th>
            <th>Конец</th>
            <th>Статус</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {items.map((slot) => (
            <tr key={slot.id}>
              <td>{formatDateTime(slot.startTime)}</td>
              <td>{formatDateTime(slot.endTime)}</td>
              <td>{slotStatusLabel(slot.status)}</td>
              <td>
                <div className="actions">
                  {slot.status === 'Available' ? (
                    <>
                      <button
                        type="button"
                        className="btn btn-ghost-dark"
                        disabled={busy}
                        onClick={() => openEdit(slot)}
                      >
                        Изменить
                      </button>
                      <button
                        type="button"
                        className="btn btn-danger"
                        disabled={busy}
                        onClick={() => void onDelete(slot.id)}
                      >
                        Удалить
                      </button>
                    </>
                  ) : (
                    <span className="muted">—</span>
                  )}
                </div>
              </td>
            </tr>
          ))}
          {items.length === 0 && (
            <tr>
              <td colSpan={4}>Слотов пока нет</td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  )
}
