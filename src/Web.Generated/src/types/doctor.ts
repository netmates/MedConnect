export type DoctorDto = {
  id: string
  keycloakId: string
  lastName: string
  firstName: string
  middleName?: string | null
  description: string
  experienceYears: number
  isActive: boolean
  specializations: string[]
}

export type CreateDoctorDto = {
  lastName: string
  firstName: string
  middleName?: string | null
  email: string
  temporaryPassword: string
  description: string
  experienceYears: number
  specializationIds: string[]
}

export type UpdateDoctorDto = {
  lastName: string
  firstName: string
  middleName?: string | null
  description: string
  experienceYears: number
  specializationIds: string[]
}

export type ResetPasswordDto = {
  newPassword: string
}
