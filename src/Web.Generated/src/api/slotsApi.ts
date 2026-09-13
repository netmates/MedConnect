import { apiFetch } from './http'
import type {
  CreateScheduleSlotDto,
  ScheduleSlotDto,
  UpdateScheduleSlotDto,
} from '../types/slot'

export const slotsApi = {
  my: () => apiFetch<ScheduleSlotDto[]>('/api/slots'),
  available: (doctorId: string, date: string) =>
    apiFetch<ScheduleSlotDto[]>(
      `/api/slots/available?doctorId=${encodeURIComponent(doctorId)}&date=${encodeURIComponent(date)}`,
    ),
  create: (dto: CreateScheduleSlotDto) =>
    apiFetch<ScheduleSlotDto>('/api/slots', {
      method: 'POST',
      body: JSON.stringify(dto),
    }),
  update: (id: string, dto: UpdateScheduleSlotDto) =>
    apiFetch<ScheduleSlotDto>(`/api/slots/${id}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    }),
  remove: (id: string) => apiFetch<void>(`/api/slots/${id}`, { method: 'DELETE' }),
}
