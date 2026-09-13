import { apiFetch } from './http'
import type { PatientDto, UpdatePatientDto } from '../types/patient'

export type RegisterPatientDto = UpdatePatientDto

export const patientProfileApi = {
  register: (dto: RegisterPatientDto) =>
    apiFetch<PatientDto>('/api/patients/register', {
      method: 'POST',
      body: JSON.stringify(dto),
    }),
  me: () => apiFetch<PatientDto>('/api/patients/me'),
  updateMe: (dto: UpdatePatientDto) =>
    apiFetch<PatientDto>('/api/patients/me', {
      method: 'PUT',
      body: JSON.stringify(dto),
    }),
}
