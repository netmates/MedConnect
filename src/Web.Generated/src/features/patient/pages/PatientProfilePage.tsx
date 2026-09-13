import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../../../api/http'
import { patientProfileApi } from '../../../api/patientProfileApi'
import { formatApiError } from '../../../lib/format'
import type { PatientDto, UpdatePatientDto } from '../../../types/patient'

const emptyForm: UpdatePatientDto = {
  lastName: '',
  firstName: '',
  middleName: '',
  phone: '',
  dateOfBirth: '',
}

function toDateInput(value?: string | null): string {
  if (!value) return ''
  return value.slice(0, 10)
}

export function PatientProfilePage() {
  const [profile, setProfile] = useState<PatientDto | null>(null)
  const [needsRegister, setNeedsRegister] = useState(false)
  const [form, setForm] = useState<UpdatePatientDto>(emptyForm)
  const [error, setError] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      const me = await patientProfileApi.me()
      setProfile(me)
      setNeedsRegister(false)
      setForm({
        lastName: me.lastName,
        firstName: me.firstName,
        middleName: me.middleName ?? '',
        phone: me.phone ?? '',
        dateOfBirth: toDateInput(me.dateOfBirth),
      })
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) {
        setNeedsRegister(true)
        setProfile(null)
        setForm(emptyForm)
        return
      }
      setError(formatApiError(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setOk(null)
    const payload: UpdatePatientDto = {
      ...form,
      middleName: form.middleName?.trim() || null,
      phone: form.phone?.trim() || null,
      dateOfBirth: form.dateOfBirth?.trim() || null,
    }
    try {
      const saved = needsRegister
        ? await patientProfileApi.register(payload)
        : await patientProfileApi.updateMe(payload)
      setProfile(saved)
      setNeedsRegister(false)
      setOk(needsRegister ? 'Профиль создан' : 'Сохранено')
      setForm({
        lastName: saved.lastName,
        firstName: saved.firstName,
        middleName: saved.middleName ?? '',
        phone: saved.phone ?? '',
        dateOfBirth: toDateInput(saved.dateOfBirth),
      })
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="panel panel-wide">
      <h1>Профиль</h1>
      <p className="lead">
        {needsRegister
          ? 'Профиля еще нет — заполните данные для регистрации в системе.'
          : 'Редактирование данных пациента.'}
      </p>

      {error && <p className="error-banner">{error}</p>}
      {ok && <p className="ok">{ok}</p>}

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
            {needsRegister ? 'Зарегистрировать профиль' : 'Сохранить'}
          </button>
        </div>
      </form>

      {profile && (
        <p className="lead">
          Статус: {profile.isActive ? 'активен' : 'неактивен'} · id: {profile.id}
        </p>
      )}
    </section>
  )
}
