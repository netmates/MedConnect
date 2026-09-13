export type AppointmentStatus = 'Created' | 'Confirmed' | 'Cancelled' | 'Completed'

export type AppointmentDto = {
  id: string
  patientId: string
  doctorId: string
  slotId: string
  reason?: string | null
  status: string
  createdAt: string
  updatedAt: string
  doctorFullName: string
  patientFullName: string
  startTime: string
  endTime: string
}

export type CreateAppointmentDto = {
  slotId: string
  reason?: string | null
}
