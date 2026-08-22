# MedConnect

Локальный запуск инфраструктуры и **AppointmentService** (.NET).

## Требования

- Docker Desktop (или совместимый Docker Engine + Compose)
- .NET SDK 10
- Node.js 24 LTS (для админ-фронта)
- Git

## 1. Клонирование и инфраструктура

```powershell
git clone <repo-url>
cd MedConnect
docker compose up -d
```

Поднимаются:

| Сервис   | URL / порт              | Назначение                          |
|----------|-------------------------|-------------------------------------|
| Postgres | `localhost:5432`        | БД `medconnect` / `postgres`/`postgres` |
| Keycloak | http://localhost:8080   | admin / admin                       |
| Seq      | http://localhost:5341   | UI логов (Dev)                      |

Дождитесь готовности Keycloak (первый старт и импорт realm могут занять минуту).

## 2. Secret для Admin API Keycloak (обязательно на каждой машине)

Клиент `medconnect-admin-cli` используется AppointmentService для создания/блокировки пользователей.  
**Secret не хранится в git** — его задают локально после старта Keycloak.

### 2.1. Скопировать secret из Keycloak

1. Откройте http://localhost:8080 → войти `admin` / `admin`.
2. Realm **medconnect** (не master).
3. **Clients** → `medconnect-admin-cli` → вкладка **Credentials**.
4. Скопируйте **Client secret** (при необходимости нажмите Regenerate и снова скопируйте).

У каждого разработчика свой локальный Keycloak (свой Docker volume) → **свой** secret.  
Общий секрет на команду не нужен и в репозиторий не коммитится.

### 2.2. Сохранить в .NET User Secrets

```powershell
cd src/Services/AppointmentService
dotnet user-secrets set "Keycloak:AdminClientSecret" "<вставьте_secret_сюда>"
```

## 3. Запуск AppointmentService

```powershell
cd src/Services/AppointmentService
dotnet run
```

По умолчанию профиль Development:

- HTTP: http://localhost:5067  
- HTTPS: https://localhost:7246  
- OpenAPI / Scalar: `/scalar` (в Development)

Миграции EF и seed применяются при старте.  
В Development включён `Seed:DemoUsers` (`appsettings.Development.json`) — нужны рабочий Keycloak и корректный `AdminClientSecret`.

## 4. Логирование

- **Console** — при `dotnet run` (формат и уровни в `appsettings*.json`).
- **Seq** — только в Development (`LoggingExtensions` → `http://localhost:5341`).
- В логах: `ServiceName`, `EnvironmentName`, `CorrelationId`, после JWT — `UserId`.

Заголовок запроса/ответа: `X-Correlation-ID`.

## 5. Админ-фронт (`src/Web/admin`)

Vite + React + TypeScript. Вход через Keycloak (**Authorization Code + PKCE**), клиент `medconnect-app`.

```powershell
cd src/Web/admin
npm install
npm run dev
```

UI: http://localhost:3000 (порт задан в `vite.config.ts`, совпадает с `redirectUris` / `webOrigins` в realm).

Нужны запущенные Keycloak и (для API позже) AppointmentService. Логин тестового админа из realm-export: `admin1` / `Admin1Pass!` (роль `admin`).

Маршруты сейчас: `/login`, `/auth/callback`, `/` (после входа), `/access-denied`.

## 6. Тесты

```powershell
cd tests/AppointmentService.UnitTests
dotnet test
```

## 7. Чеклист для нового ПК

1. [ ] Установлены Docker, .NET 10 и Node.js 24  
2. [ ] `docker compose up -d`  
3. [ ] Keycloak доступен на :8080, realm `medconnect`  
4. [ ] Скопирован Client secret `medconnect-admin-cli`  
5. [ ] `dotnet user-secrets set "Keycloak:AdminClientSecret" "..."`  
6. [ ] `dotnet run` в `AppointmentService`  
7. [ ] `npm install` + `npm run dev` в `src/Web/admin`  
8. [ ] (опционально) Seq UI на :5341  
