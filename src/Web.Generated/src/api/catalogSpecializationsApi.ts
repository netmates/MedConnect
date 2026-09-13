import { apiFetch } from './http'
import type { SpecializationDto } from '../types/specialization'

export const catalogSpecializationsApi = {
  list: () => apiFetch<SpecializationDto[]>('/api/specializations'),
}
