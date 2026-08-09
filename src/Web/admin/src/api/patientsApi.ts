import { apiFetch } from './http'
import type { PatientDto, UpdatePatientDto } from '../types/patient'

const base = '/api/admin/patients'

export const patientsApi = {
  list: () => apiFetch<PatientDto[]>(base),
  get: (id: string) => apiFetch<PatientDto>(`${base}/${id}`),
  update: (id: string, dto: UpdatePatientDto) =>
    apiFetch<PatientDto>(`${base}/${id}`, { method: 'PUT', body: JSON.stringify(dto) }),
  deactivate: (id: string) => apiFetch<void>(`${base}/${id}/deactivate`, { method: 'POST' }),
  activate: (id: string) => apiFetch<void>(`${base}/${id}/activate`, { method: 'POST' }),
}
