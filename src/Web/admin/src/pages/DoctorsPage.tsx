import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { doctorsApi } from '../api/doctorsApi'
import { specializationsApi } from '../api/specializationsApi'
import { formatApiError, fullName } from '../lib/format'
import type { CreateDoctorDto, DoctorDto, UpdateDoctorDto } from '../types/doctor'
import type { SpecializationDto } from '../types/specialization'

const emptyCreate: CreateDoctorDto = {
  lastName: '',
  firstName: '',
  middleName: '',
  email: '',
  temporaryPassword: '',
  description: '',
  experienceYears: 0,
  specializationIds: [],
}

type Mode = 'create' | 'edit' | 'password' | null

export function DoctorsPage() {
  const [items, setItems] = useState<DoctorDto[]>([])
  const [specs, setSpecs] = useState<SpecializationDto[]>([])
  const [mode, setMode] = useState<Mode>(null)
  const [selected, setSelected] = useState<DoctorDto | null>(null)
  const [createForm, setCreateForm] = useState<CreateDoctorDto>(emptyCreate)
  const [editForm, setEditForm] = useState<UpdateDoctorDto | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const load = useCallback(async () => {
    setError(null)
    try {
      const [doctors, specializations] = await Promise.all([
        doctorsApi.list(),
        specializationsApi.list(),
      ])
      setItems(doctors)
      setSpecs(specializations)
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const specNameToId = useMemo(() => {
    const map = new Map<string, string>()
    for (const s of specs) map.set(s.name, s.id)
    return map
  }, [specs])

  function openCreate() {
    setMode('create')
    setSelected(null)
    setCreateForm({ ...emptyCreate, specializationIds: [] })
  }

  function openEdit(doctor: DoctorDto) {
    const ids = doctor.specializations
      .map((name) => specNameToId.get(name))
      .filter((id): id is string => Boolean(id))

    setSelected(doctor)
    setEditForm({
      lastName: doctor.lastName,
      firstName: doctor.firstName,
      middleName: doctor.middleName ?? '',
      description: doctor.description,
      experienceYears: doctor.experienceYears,
      specializationIds: ids,
    })
    setMode('edit')
  }

  function openPassword(doctor: DoctorDto) {
    setSelected(doctor)
    setNewPassword('')
    setMode('password')
  }

  function toggleSpec(id: string, current: string[], setter: (ids: string[]) => void) {
    setter(
      current.includes(id) ? current.filter((x) => x !== id) : [...current, id],
    )
  }

  async function submitCreate(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      await doctorsApi.create({
        ...createForm,
        middleName: createForm.middleName?.trim() || null,
      })
      setMode(null)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function submitEdit(e: FormEvent) {
    e.preventDefault()
    if (!selected || !editForm) return
    setBusy(true)
    setError(null)
    try {
      await doctorsApi.update(selected.id, {
        ...editForm,
        middleName: editForm.middleName?.trim() || null,
      })
      setMode(null)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function submitPassword(e: FormEvent) {
    e.preventDefault()
    if (!selected) return
    setBusy(true)
    setError(null)
    try {
      await doctorsApi.resetPassword(selected.id, { newPassword })
      setMode(null)
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  async function toggleActive(doctor: DoctorDto) {
    const action = doctor.isActive ? 'деактивировать' : 'активировать'
    if (!confirm(`Точно ${action} врача?`)) return
    setBusy(true)
    setError(null)
    try {
      if (doctor.isActive) await doctorsApi.deactivate(doctor.id)
      else await doctorsApi.activate(doctor.id)
      await load()
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="panel panel-wide">
      <div className="panel-head">
        <div>
          <h1>Врачи</h1>
          <p className="lead">Создание в Keycloak + профиль в БД.</p>
        </div>
        <button type="button" className="btn btn-primary" onClick={openCreate} disabled={busy}>
          Создать врача
        </button>
      </div>

      {error && <p className="error-banner">{error}</p>}

      {mode === 'create' && (
        <form className="form-grid" onSubmit={(e) => void submitCreate(e)}>
          <h2>Новый врач</h2>
          <DoctorFields
            lastName={createForm.lastName}
            firstName={createForm.firstName}
            middleName={createForm.middleName ?? ''}
            description={createForm.description}
            experienceYears={createForm.experienceYears}
            specializationIds={createForm.specializationIds}
            specs={specs}
            onChange={(patch) => setCreateForm((prev) => ({ ...prev, ...patch }))}
            onToggleSpec={(id) =>
              toggleSpec(id, createForm.specializationIds, (specializationIds) =>
                setCreateForm((prev) => ({ ...prev, specializationIds })),
              )
            }
          />
          <label>
            Email
            <input
              className="input"
              type="email"
              required
              value={createForm.email}
              onChange={(e) => setCreateForm((p) => ({ ...p, email: e.target.value }))}
            />
          </label>
          <label>
            Временный пароль
            <input
              className="input"
              type="password"
              required
              value={createForm.temporaryPassword}
              onChange={(e) =>
                setCreateForm((p) => ({ ...p, temporaryPassword: e.target.value }))
              }
            />
          </label>
          <div className="btn-row">
            <button className="btn btn-primary" type="submit" disabled={busy}>
              Создать
            </button>
            <button className="btn btn-ghost-dark" type="button" onClick={() => setMode(null)}>
              Отмена
            </button>
          </div>
        </form>
      )}

      {mode === 'edit' && editForm && selected && (
        <form className="form-grid" onSubmit={(e) => void submitEdit(e)}>
          <h2>Редактирование: {fullName(selected.lastName, selected.firstName, selected.middleName)}</h2>
          <DoctorFields
            lastName={editForm.lastName}
            firstName={editForm.firstName}
            middleName={editForm.middleName ?? ''}
            description={editForm.description}
            experienceYears={editForm.experienceYears}
            specializationIds={editForm.specializationIds}
            specs={specs}
            onChange={(patch) => setEditForm((prev) => (prev ? { ...prev, ...patch } : prev))}
            onToggleSpec={(id) =>
              toggleSpec(id, editForm.specializationIds, (specializationIds) =>
                setEditForm((prev) => (prev ? { ...prev, specializationIds } : prev)),
              )
            }
          />
          <div className="btn-row">
            <button className="btn btn-primary" type="submit" disabled={busy}>
              Сохранить
            </button>
            <button className="btn btn-ghost-dark" type="button" onClick={() => setMode(null)}>
              Отмена
            </button>
          </div>
        </form>
      )}

      {mode === 'password' && selected && (
        <form className="form-grid" onSubmit={(e) => void submitPassword(e)}>
          <h2>Сброс пароля: {fullName(selected.lastName, selected.firstName, selected.middleName)}</h2>
          <label>
            Новый пароль
            <input
              className="input"
              type="password"
              required
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
          </label>
          <div className="btn-row">
            <button className="btn btn-primary" type="submit" disabled={busy}>
              Сбросить
            </button>
            <button className="btn btn-ghost-dark" type="button" onClick={() => setMode(null)}>
              Отмена
            </button>
          </div>
        </form>
      )}

      <table className="table">
        <thead>
          <tr>
            <th>ФИО</th>
            <th>Стаж</th>
            <th>Специализации</th>
            <th>Статус</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {items.map((doctor) => (
            <tr key={doctor.id}>
              <td>{fullName(doctor.lastName, doctor.firstName, doctor.middleName)}</td>
              <td>{doctor.experienceYears}</td>
              <td>{doctor.specializations.join(', ') || '—'}</td>
              <td>{doctor.isActive ? 'активен' : 'неактивен'}</td>
              <td className="actions">
                <button type="button" className="btn btn-ghost-dark" disabled={busy} onClick={() => openEdit(doctor)}>
                  Изменить
                </button>
                <button type="button" className="btn btn-ghost-dark" disabled={busy} onClick={() => openPassword(doctor)}>
                  Пароль
                </button>
                <button type="button" className="btn btn-danger" disabled={busy} onClick={() => void toggleActive(doctor)}>
                  {doctor.isActive ? 'Деактивировать' : 'Активировать'}
                </button>
              </td>
            </tr>
          ))}
          {items.length === 0 && (
            <tr>
              <td colSpan={5}>Пока нет врачей</td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  )
}

type DoctorFieldsProps = {
  lastName: string
  firstName: string
  middleName: string
  description: string
  experienceYears: number
  specializationIds: string[]
  specs: SpecializationDto[]
  onChange: (patch: Partial<UpdateDoctorDto>) => void
  onToggleSpec: (id: string) => void
}

function DoctorFields({
  lastName,
  firstName,
  middleName,
  description,
  experienceYears,
  specializationIds,
  specs,
  onChange,
  onToggleSpec,
}: DoctorFieldsProps) {
  return (
    <>
      <label>
        Фамилия
        <input className="input" required value={lastName} onChange={(e) => onChange({ lastName: e.target.value })} />
      </label>
      <label>
        Имя
        <input className="input" required value={firstName} onChange={(e) => onChange({ firstName: e.target.value })} />
      </label>
      <label>
        Отчество
        <input className="input" value={middleName} onChange={(e) => onChange({ middleName: e.target.value })} />
      </label>
      <label>
        Описание
        <textarea
          className="input"
          required
          rows={3}
          value={description}
          onChange={(e) => onChange({ description: e.target.value })}
        />
      </label>
      <label>
        Стаж (лет)
        <input
          className="input"
          type="number"
          min={0}
          required
          value={experienceYears}
          onChange={(e) => onChange({ experienceYears: Number(e.target.value) })}
        />
      </label>
      <fieldset className="spec-list">
        <legend>Специализации</legend>
        {specs.map((spec) => (
          <label key={spec.id} className="check-row">
            <input
              type="checkbox"
              checked={specializationIds.includes(spec.id)}
              onChange={() => onToggleSpec(spec.id)}
            />
            {spec.name}
          </label>
        ))}
        {specs.length === 0 && <p>Сначала создайте специализации.</p>}
      </fieldset>
    </>
  )
}
