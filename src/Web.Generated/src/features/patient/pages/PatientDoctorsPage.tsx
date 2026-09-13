import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { appointmentsApi } from '../../../api/appointmentsApi'
import { catalogDoctorsApi } from '../../../api/catalogDoctorsApi'
import { catalogSpecializationsApi } from '../../../api/catalogSpecializationsApi'
import { slotsApi } from '../../../api/slotsApi'
import {
  formatApiError,
  formatDateTime,
  fullName,
  slotStatusLabel,
  todayDateInput,
} from '../../../lib/format'
import type { DoctorDto } from '../../../types/doctor'
import type { ScheduleSlotDto } from '../../../types/slot'
import type { SpecializationDto } from '../../../types/specialization'

export function PatientDoctorsPage() {
  const [specs, setSpecs] = useState<SpecializationDto[]>([])
  const [doctors, setDoctors] = useState<DoctorDto[]>([])
  const [specId, setSpecId] = useState('')
  const [selectedDoctor, setSelectedDoctor] = useState<DoctorDto | null>(null)
  const [date, setDate] = useState(todayDateInput())
  const [slots, setSlots] = useState<ScheduleSlotDto[]>([])
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const loadCatalog = useCallback(async () => {
    setError(null)
    try {
      const [specializations, list] = await Promise.all([
        catalogSpecializationsApi.list(),
        catalogDoctorsApi.list(specId || undefined),
      ])
      setSpecs(specializations)
      setDoctors(list)
    } catch (e) {
      setError(formatApiError(e))
    }
  }, [specId])

  useEffect(() => {
    void loadCatalog()
  }, [loadCatalog])

  async function loadSlots(doctor: DoctorDto, day: string) {
    setError(null)
    setOk(null)
    try {
      setSlots(await slotsApi.available(doctor.id, day))
    } catch (e) {
      setError(formatApiError(e))
      setSlots([])
    }
  }

  async function openDoctor(doctor: DoctorDto) {
    setSelectedDoctor(doctor)
    setReason('')
    await loadSlots(doctor, date)
  }

  async function onDateChange(value: string) {
    setDate(value)
    if (selectedDoctor) await loadSlots(selectedDoctor, value)
  }

  async function book(slotId: string, e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setOk(null)
    try {
      await appointmentsApi.create({
        slotId,
        reason: reason.trim() || null,
      })
      setOk('Запись создана')
      if (selectedDoctor) await loadSlots(selectedDoctor, date)
    } catch (err) {
      setError(formatApiError(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="panel panel-wide">
      <h1>Врачи и запись</h1>
      <p className="lead">
        Выберите специалиста и свободный слот. Перед записью нужен{' '}
        <Link to="/patient/profile">профиль пациента</Link>.
      </p>

      {error && <p className="error-banner">{error}</p>}
      {ok && <p className="ok">{ok}</p>}

      <div className="form-row">
        <label>
          Специализация
          <select
            className="input"
            value={specId}
            onChange={(e) => {
              setSpecId(e.target.value)
              setSelectedDoctor(null)
              setSlots([])
            }}
          >
            <option value="">Все</option>
            {specs.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <table className="table">
        <thead>
          <tr>
            <th>ФИО</th>
            <th>Стаж</th>
            <th>Специализации</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {doctors.map((doctor) => (
            <tr key={doctor.id}>
              <td>{fullName(doctor.lastName, doctor.firstName, doctor.middleName)}</td>
              <td>{doctor.experienceYears}</td>
              <td>{doctor.specializations.join(', ') || '—'}</td>
              <td><div className="actions">
                <button
                  type="button"
                  className="btn btn-ghost-dark"
                  onClick={() => void openDoctor(doctor)}
                >
                  Слоты
                </button>
                </div>
              </td>
            </tr>
          ))}
          {doctors.length === 0 && (
            <tr>
              <td colSpan={4}>Нет активных врачей</td>
            </tr>
          )}
        </tbody>
      </table>

      {selectedDoctor && (
        <div className="form-grid">
          <h2>
            Слоты: {fullName(selectedDoctor.lastName, selectedDoctor.firstName, selectedDoctor.middleName)}
          </h2>
          <p>{selectedDoctor.description}</p>
          <label>
            Дата
            <input
              className="input"
              type="date"
              value={date}
              onChange={(e) => void onDateChange(e.target.value)}
            />
          </label>
          <label>
            Причина визита (необязательно)
            <input
              className="input"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
            />
          </label>
          <table className="table">
            <thead>
              <tr>
                <th>Время</th>
                <th>Статус</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {slots.map((slot) => (
                <tr key={slot.id}>
                  <td>
                    {formatDateTime(slot.startTime)} — {formatDateTime(slot.endTime)}
                  </td>
                  <td>{slotStatusLabel(slot.status)}</td>
                  <td>
                    <button
                      type="button"
                      className="btn btn-primary"
                      disabled={busy || slot.status !== 'Available'}
                      onClick={(e) => void book(slot.id, e)}
                    >
                      Записаться
                    </button>
                  </td>
                </tr>
              ))}
              {slots.length === 0 && (
                <tr>
                  <td colSpan={3}>На этот день свободных слотов нет</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
