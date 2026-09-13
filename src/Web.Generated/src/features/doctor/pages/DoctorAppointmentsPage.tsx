import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { appointmentsApi } from '../../../api/appointmentsApi'
import {
  appointmentStatusLabel,
  formatApiError,
  formatDateTime,
} from '../../../lib/format'
import type { AppointmentDto } from '../../../types/appointment'

export function DoctorAppointmentsPage() {
  const [items, setItems] = useState<AppointmentDto[]>([])
  const [status, setStatus] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await appointmentsApi.myAsDoctor({ status: status || undefined }))
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [status])

  useEffect(() => {
    void load()
  }, [load])

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError(null)
    try {
      await action()
      await load()
    } catch (e) {
      setError(formatApiError(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="panel panel-wide">
      <h1>Приемы</h1>
      <p className="lead">Подтверждение, завершение, отмена и чат с пациентом.</p>

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
            <th>Пациент</th>
            <th>Время</th>
            <th>Статус</th>
            <th>Причина</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>{item.patientFullName}</td>
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
                      to={`/doctor/appointments/${item.id}/chat`}
                    >
                      Чат
                    </Link>
                    {item.status === 'Created' && (
                      <button
                        type="button"
                        className="btn btn-primary"
                        disabled={busy}
                        onClick={() => void run(() => appointmentsApi.confirm(item.id))}
                      >
                        Подтвердить
                      </button>
                    )}
                    {item.status === 'Confirmed' && (
                      <button
                        type="button"
                        className="btn btn-primary"
                        disabled={busy}
                        onClick={() => void run(() => appointmentsApi.complete(item.id))}
                      >
                        Завершить
                      </button>
                    )}
                    <button
                      type="button"
                      className="btn btn-danger"
                      disabled={busy}
                      onClick={() => {
                        if (!confirm('Отменить прием?')) return
                        void run(() => appointmentsApi.cancel(item.id))
                      }}
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
              <td colSpan={5}>Приемов пока нет</td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  )
}
