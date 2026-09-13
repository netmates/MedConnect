const keycloakUrl = import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080'
const realm = import.meta.env.VITE_KEYCLOAK_REALM ?? 'medconnect'

export const config = {
  apiUrl: import.meta.env.VITE_API_URL ?? '',
  keycloakUrl,
  realm,
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'medconnect-app',
  authority: `${keycloakUrl}/realms/${realm}`,
  redirectUri: `${window.location.origin}/auth/callback`,
  postLogoutRedirectUri: `${window.location.origin}/login`,
} as const
