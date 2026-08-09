import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { specializationsApi } from '../api/specializationsApi'
import { formatApiError } from '../lib/format'
import type { SpecializationDto } from '../types/specialization'

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
    <section className="panel panel-wide">
      <h1>Специализации</h1>
      <p className="lead">Справочник для профилей врачей.</p>

      {error && <p className="error-banner">{error}</p>}

      <form className="form-row" onSubmit={(e) => void onSubmit(e)}>
        <input
          className="input"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Название"
          required
        />
        <button className="btn btn-primary" type="submit" disabled={busy}>
          {editingId ? 'Сохранить' : 'Добавить'}
        </button>
        {editingId && (
          <button
            className="btn btn-ghost-dark"
            type="button"
            disabled={busy}
            onClick={() => {
              setEditingId(null)
              setName('')
            }}
          >
            Отмена
          </button>
        )}
      </form>

      <table className="table">
        <thead>
          <tr>
            <th>Название</th>
            <th>Id</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>{item.name}</td>
              <td className="mono">{item.id}</td>
              <td className="actions">
                <button
                  type="button"
                  className="btn btn-ghost-dark"
                  disabled={busy}
                  onClick={() => {
                    setEditingId(item.id)
                    setName(item.name)
                  }}
                >
                  Изменить
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  disabled={busy}
                  onClick={() => void onDelete(item.id)}
                >
                  Удалить
                </button>
              </td>
            </tr>
          ))}
          {items.length === 0 && (
            <tr>
              <td colSpan={3}>Пока пусто</td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  )
}
