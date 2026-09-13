export type ScheduleSlotDto = {
  id: string
  doctorId: string
  startTime: string
  endTime: string
  status: string
  createdAt: string
  updatedAt: string
}

export type CreateScheduleSlotDto = {
  startTime: string
  endTime: string
}

export type UpdateScheduleSlotDto = {
  startTime: string
  endTime: string
}
