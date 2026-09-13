export type PatientDto = {
  id: string
  keycloakId: string
  lastName: string
  firstName: string
  middleName?: string | null
  phone?: string | null
  dateOfBirth?: string | null
  isActive: boolean
  createdAt: string
}

export type UpdatePatientDto = {
  lastName: string
  firstName: string
  middleName?: string | null
  phone?: string | null
  dateOfBirth?: string | null
}
