import { apiFetch } from './http'
import type { AppointmentDto, CreateAppointmentDto } from '../types/appointment'

function query(params: { status?: string; from?: string; to?: string }): string {
  const search = new URLSearchParams()
  if (params.status) search.set('status', params.status)
  if (params.from) search.set('from', params.from)
  if (params.to) search.set('to', params.to)
  const q = search.toString()
  return q ? `?${q}` : ''
}

export const appointmentsApi = {
  myAsPatient: (params: { status?: string; from?: string; to?: string } = {}) =>
    apiFetch<AppointmentDto[]>(`/api/appointments/my${query(params)}`),
  myAsDoctor: (params: { status?: string; from?: string; to?: string } = {}) =>
    apiFetch<AppointmentDto[]>(`/api/appointments/doctor/my${query(params)}`),
  get: (id: string) => apiFetch<AppointmentDto>(`/api/appointments/${id}`),
  create: (dto: CreateAppointmentDto) =>
    apiFetch<AppointmentDto>('/api/appointments', {
      method: 'POST',
      body: JSON.stringify(dto),
    }),
  cancel: (id: string) =>
    apiFetch<void>(`/api/appointments/${id}/cancel`, { method: 'POST' }),
  confirm: (id: string) =>
    apiFetch<void>(`/api/appointments/${id}/confirm`, { method: 'POST' }),
  complete: (id: string) =>
    apiFetch<void>(`/api/appointments/${id}/complete`, { method: 'POST' }),
}
