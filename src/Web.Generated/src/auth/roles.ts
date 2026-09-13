/** Достает realm-роли из access token Keycloak (claim realm_access.roles). */
export function getRealmRolesFromAccessToken(accessToken: string | undefined): string[] {
  if (!accessToken) return []

  try {
    const payloadPart = accessToken.split('.')[1]
    if (!payloadPart) return []

    const json = atob(payloadPart.replace(/-/g, '+').replace(/_/g, '/'))
    const payload = JSON.parse(json) as {
      realm_access?: { roles?: string[] }
    }

    return payload.realm_access?.roles ?? []
  } catch {
    return []
  }
}

export function hasAdminRole(roles: string[]): boolean {
  return roles.includes('admin')
}

export function hasDoctorRole(roles: string[]): boolean {
  return roles.includes('doctor')
}

export function hasPatientRole(roles: string[]): boolean {
  return roles.includes('patient')
}

/** Приоритет: admin → doctor → patient. */
export function homePathForRoles(roles: string[]): string {
  if (hasAdminRole(roles)) return '/admin'
  if (hasDoctorRole(roles)) return '/doctor'
  if (hasPatientRole(roles)) return '/patient'
  return '/access-denied'
}
