import { apiFetch } from './http'
import type {
  CreateDoctorDto,
  DoctorDto,
  ResetPasswordDto,
  UpdateDoctorDto,
} from '../types/doctor'

const base = '/api/admin/doctors'

export const doctorsApi = {
  list: () => apiFetch<DoctorDto[]>(base),
  create: (dto: CreateDoctorDto) =>
    apiFetch<DoctorDto>(base, { method: 'POST', body: JSON.stringify(dto) }),
  update: (id: string, dto: UpdateDoctorDto) =>
    apiFetch<DoctorDto>(`${base}/${id}`, { method: 'PUT', body: JSON.stringify(dto) }),
  deactivate: (id: string) => apiFetch<void>(`${base}/${id}/deactivate`, { method: 'POST' }),
  activate: (id: string) => apiFetch<void>(`${base}/${id}/activate`, { method: 'POST' }),
  resetPassword: (id: string, dto: ResetPasswordDto) =>
    apiFetch<void>(`${base}/${id}/reset-password`, {
      method: 'POST',
      body: JSON.stringify(dto),
    }),
}
