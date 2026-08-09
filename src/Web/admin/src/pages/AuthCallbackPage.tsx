import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { userManager } from '../auth/userManager'
import { getRealmRolesFromAccessToken, hasAdminRole } from '../auth/roles'

export function AuthCallbackPage() {
  const navigate = useNavigate()
  const started = useRef(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (started.current) return
    started.current = true

    ;(async () => {
      try {
        const user = await userManager.signinRedirectCallback()
        const roles = getRealmRolesFromAccessToken(user.access_token)
        navigate(hasAdminRole(roles) ? '/' : '/access-denied', { replace: true })
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Ошибка входа')
      }
    })()
  }, [navigate])

  if (error) {
    return (
      <div className="auth-page">
        <div className="auth-card">
          <h1>Не удалось войти</h1>
          <p className="lead">{error}</p>
          <button type="button" className="btn btn-primary" onClick={() => navigate('/login')}>
            На страницу входа
          </button>
        </div>
      </div>
    )
  }

  return <p className="page-status">Завершение входа…</p>
}
