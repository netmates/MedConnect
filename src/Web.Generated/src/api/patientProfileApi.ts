import { apiFetch } from './http'
import type { PatientDto, SignUpPatientDto, UpdatePatientDto } from '../types/patient'

export const patientProfileApi = {
  signUp: (dto: SignUpPatientDto) =>
    apiFetch<PatientDto>(
      '/api/patients/sign-up',
      { method: 'POST', body: JSON.stringify(dto) },
      { anonymous: true },
    ),
  me: () => apiFetch<PatientDto>('/api/patients/me'),
  updateMe: (dto: UpdatePatientDto) =>
    apiFetch<PatientDto>('/api/patients/me', {
      method: 'PUT',
      body: JSON.stringify(dto),
    }),
}
