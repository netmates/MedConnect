import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { appointmentsApi } from '../../../api/appointmentsApi'
import {
  appointmentStatusLabel,
  formatApiError,
  formatDateTime,
} from '../../../lib/format'
import type { AppointmentDto } from '../../../types/appointment'

export function PatientAppointmentsPage() {
  const [items, setItems] = useState<AppointmentDto[]>([])
  const [status, setStatus] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await appointmentsApi.myAsPatient({ status: status || undefined }))
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [status])

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
    <section className="panel panel-wide">
      <h1>Мои записи</h1>
      <p className="lead">Записи к врачам и переход в чат по приему.</p>

      {error && <p className="error-banner">{error}</p>}

      <div className="form-row">
        <label>
          Статус записи
          <select className="input" value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">Все</option>
            <option value="Created">Создана</option>
            <option value="Confirmed">Подтверждена</option>
            <option value="Cancelled">Отменена</option>
            <option value="Completed">Завершена</option>
          </select>
        </label>
      </div>

      <table className="table">
        <thead>
          <tr>
            <th>Врач</th>
            <th>Время</th>
            <th>Статус</th>
            <th>Причина</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>{item.doctorFullName}</td>
              <td>
                {formatDateTime(item.startTime)} — {formatDateTime(item.endTime)}
              </td>
              <td>{appointmentStatusLabel(item.status)}</td>
              <td>{item.reason ?? '—'}</td>
              <td>
                <div className="actions">
                  {item.status !== 'Cancelled' && item.status !== 'Completed' ? (
                    <>
                      <Link
                        className="btn btn-ghost-dark"
                        to={`/patient/appointments/${item.id}/chat`}
                      >
                        Чат
                      </Link>
                      <button
                        type="button"
                        className="btn btn-danger"
                        disabled={busy}
                        onClick={() => void cancel(item.id)}
                      >
                        Отменить
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
              <td colSpan={5}>Записей пока нет</td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  )
}
