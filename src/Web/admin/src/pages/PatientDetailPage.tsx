import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { patientsApi } from '../api/patientsApi'
import { formatApiError, fullName } from '../lib/format'
import type { PatientDto, UpdatePatientDto } from '../types/patient'

function toDateInput(value?: string | null): string {
  if (!value) return ''
  return value.slice(0, 10)
}

export function PatientDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [patient, setPatient] = useState<PatientDto | null>(null)
  const [form, setForm] = useState<UpdatePatientDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    if (!id) return
    setError(null)
    try {
      const data = await patientsApi.get(id)
      setPatient(data)
      setForm({
        lastName: data.lastName,
        firstName: data.firstName,
        middleName: data.middleName ?? '',
        phone: data.phone ?? '',
        dateOfBirth: toDateInput(data.dateOfBirth),
      })
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    if (!id || !form) return
    setBusy(true)
    setError(null)
    try {
      const updated = await patientsApi.update(id, {
        ...form,
        middleName: form.middleName?.trim() || null,
        phone: form.phone?.trim() || null,
        dateOfBirth: form.dateOfBirth?.trim() || null,
      })
      setPatient(updated)
      setForm({
        lastName: updated.lastName,
        firstName: updated.firstName,
        middleName: updated.middleName ?? '',
        phone: updated.phone ?? '',
        dateOfBirth: toDateInput(updated.dateOfBirth),
      })
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function toggleActive() {
    if (!patient) return
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

  if (!form || !patient) {
    return (
      <section className="panel">
        {error ? <p className="error-banner">{error}</p> : <p className="page-status">Загрузка…</p>}
        <Link to="/patients">← К списку</Link>
      </section>
    )
  }

  return (
    <section className="panel panel-wide">
      <p>
        <Link to="/patients">← К списку</Link>
      </p>
      <h1>{fullName(patient.lastName, patient.firstName, patient.middleName)}</h1>
      <p className="lead">
        Статус: {patient.isActive ? 'активен' : 'неактивен'} · KeycloakId: {patient.keycloakId}
      </p>

      {error && <p className="error-banner">{error}</p>}

      <form className="form-grid" onSubmit={(e) => void onSubmit(e)}>
        <label>
          Фамилия
          <input
            className="input"
            required
            value={form.lastName}
            onChange={(e) => setForm({ ...form, lastName: e.target.value })}
          />
        </label>
        <label>
          Имя
          <input
            className="input"
            required
            value={form.firstName}
            onChange={(e) => setForm({ ...form, firstName: e.target.value })}
          />
        </label>
        <label>
          Отчество
          <input
            className="input"
            value={form.middleName ?? ''}
            onChange={(e) => setForm({ ...form, middleName: e.target.value })}
          />
        </label>
        <label>
          Телефон
          <input
            className="input"
            value={form.phone ?? ''}
            onChange={(e) => setForm({ ...form, phone: e.target.value })}
          />
        </label>
        <label>
          Дата рождения
          <input
            className="input"
            type="date"
            value={form.dateOfBirth ?? ''}
            onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
          />
        </label>
        <div className="btn-row">
          <button className="btn btn-primary" type="submit" disabled={busy}>
            Сохранить
          </button>
          <button className="btn btn-danger" type="button" disabled={busy} onClick={() => void toggleActive()}>
            {patient.isActive ? 'Деактивировать' : 'Активировать'}
          </button>
        </div>
      </form>
    </section>
  )
}
