import { apiFetch } from './http'
import type {
  CreateSpecializationDto,
  SpecializationDto,
  UpdateSpecializationDto,
} from '../types/specialization'

const base = '/api/admin/specializations'

export const specializationsApi = {
  list: () => apiFetch<SpecializationDto[]>(base),
  create: (dto: CreateSpecializationDto) =>
    apiFetch<SpecializationDto>(base, { method: 'POST', body: JSON.stringify(dto) }),
  update: (id: string, dto: UpdateSpecializationDto) =>
    apiFetch<SpecializationDto>(`${base}/${id}`, { method: 'PUT', body: JSON.stringify(dto) }),
  remove: (id: string) => apiFetch<void>(`${base}/${id}`, { method: 'DELETE' }),
}
