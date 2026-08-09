import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { patientsApi } from '../api/patientsApi'
import { formatApiError, fullName } from '../lib/format'
import type { PatientDto } from '../types/patient'

export function PatientsPage() {
  const [items, setItems] = useState<PatientDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      setItems(await patientsApi.list())
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function toggleActive(patient: PatientDto) {
    const action = patient.isActive ? 'деактивировать' : 'активировать'
    if (!confirm(`Точно ${action} пациента?`)) return
    setBusy(true)
    setError(null)
    try {
      if (patient.isActive) await patientsApi.deactivate(patient.id)
      else await patientsApi.activate(patient.id)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="panel panel-wide">
      <h1>Пациенты</h1>
      <p className="lead">Список профилей, включая неактивных.</p>

      {error && <p className="error-banner">{error}</p>}

      <table className="table">
        <thead>
          <tr>
            <th>ФИО</th>
            <th>Телефон</th>
            <th>Статус</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {items.map((patient) => (
            <tr key={patient.id}>
              <td>{fullName(patient.lastName, patient.firstName, patient.middleName)}</td>
              <td>{patient.phone ?? '—'}</td>
              <td>{patient.isActive ? 'активен' : 'неактивен'}</td>
              <td className="actions">
                <Link className="btn btn-ghost-dark" to={`/patients/${patient.id}`}>
                  Открыть
                </Link>
                <button
                  type="button"
                  className="btn btn-danger"
                  disabled={busy}
                  onClick={() => void toggleActive(patient)}
                >
                  {patient.isActive ? 'Деактивировать' : 'Активировать'}
                </button>
              </td>
            </tr>
          ))}
          {items.length === 0 && (
            <tr>
              <td colSpan={4}>Пока нет пациентов</td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  )
}
