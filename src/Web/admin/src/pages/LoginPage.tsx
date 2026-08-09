import { useAuth } from '../auth/AuthContext'

export function LoginPage() {
  const { login, isLoading, user } = useAuth()

  return (
    <div className="auth-page">
      <div className="auth-card">
        <p className="eyebrow">MedConnect</p>
        <h1>Вход в админку</h1>
        <p className="lead">
          Авторизация через Keycloak (Authorization Code + PKCE). Нужна роль{' '}
          <code>admin</code>.
        </p>

        {isLoading ? (
          <p className="page-status">Проверка сессии…</p>
        ) : user ? (
          <p className="page-status">Вы уже вошли. Перейдите на главную.</p>
        ) : (
          <button type="button" className="btn btn-primary" onClick={() => void login()}>
            Войти через Keycloak
          </button>
        )}
      </div>
    </div>
  )
}
