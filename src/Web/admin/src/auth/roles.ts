/** Достаёт realm-роли из access token Keycloak (claim realm_access.roles). */
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
