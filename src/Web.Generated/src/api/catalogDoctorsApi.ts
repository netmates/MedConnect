import { apiFetch } from './http'
import type { DoctorDto } from '../types/doctor'

export const catalogDoctorsApi = {
  list: (specializationId?: string) => {
    const query = specializationId
      ? `?specializationId=${encodeURIComponent(specializationId)}`
      : ''
    return apiFetch<DoctorDto[]>(`/api/doctors${query}`)
  },
  get: (id: string) => apiFetch<DoctorDto>(`/api/doctors/${id}`),
}
