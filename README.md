# MedConnect

Локальный запуск инфраструктуры, бэкенд-сервисов (.NET) и SPA (`src/Web.Generated`).

## Требования

- Docker Desktop (или совместимый Docker Engine + Compose)
- .NET SDK 10
- Node.js 24 LTS (для SPA)
- Git
- **HTTPS-сертификат разработчика ASP.NET Core — установлен и trusted** (см. ниже)

### HTTPS developer certificate (обязательно для локалки)

CommunicationService ходит в AppointmentService по **HTTPS gRPC** (`https://localhost:7246`).  
Без доверенного dev-сертификата локально ломаются gRPC/чат и другие HTTPS-вызовы между сервисами.

Один раз на машине:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Проверка:

```powershell
dotnet dev-certs https --check --trust
```

Должно быть: сертификат найден и **trusted**.  
Если Windows покажет диалог доверия — подтвердите. После смены/переустановки SDK или сертификата повторите `--trust`.

## 1. Клонирование и инфраструктура

```powershell
git clone <repo-url>
cd MedConnect
docker compose up -d
```

Поднимаются:

| Сервис   | URL / порт                         | Назначение                                      |
|----------|------------------------------------|-------------------------------------------------|
| Postgres | `localhost:5432`                   | БД `medconnect` / `postgres` / `postgres`       |
| Keycloak | http://localhost:8080              | admin / admin                                   |
| Seq      | http://localhost:5341              | UI логов (Dev)                                  |
| MongoDB  | `localhost:27017`                  | чаты (CommunicationService)                     |
| RabbitMQ | `localhost:5672`, UI `:15672`      | очереди / `medconnect` / `medconnect`           |

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

## 3. Запуск бэкенда

Нужны **trusted** HTTPS-сертификат (см. выше), Docker-инфра и AdminClientSecret.

### 3.1. AppointmentService

```powershell
cd src/Services/AppointmentService
dotnet run
```

По умолчанию профиль Development:

- HTTP: http://localhost:5067
- HTTPS: https://localhost:7246
- OpenAPI / Scalar: `/scalar` (в Development)

Миграции EF и seed применяются при старте.  
В Development включен `Seed:DemoUsers` (`appsettings.Development.json`) — нужны рабочий Keycloak и корректный `AdminClientSecret`.

### 3.2. CommunicationService

```powershell
cd src/Services/CommunicationService
dotnet run
```

- HTTP: http://localhost:5080
- HTTPS: https://localhost:7280
- OpenAPI / Scalar: `/scalar` (в Development)
- gRPC к Appointment: `https://localhost:7246` (нужен trusted cert)

## 4. SPA (`src/Web.Generated`)

Vite + React + TypeScript + **MUI** (`@mui/material`, `@mui/icons-material`).  
Вход через Keycloak (**Authorization Code + PKCE**), клиент `medconnect-app`.

Роли открывают свои зоны:

| Роль      | Базовый путь | Разделы                                      |
|-----------|--------------|----------------------------------------------|
| `admin`   | `/admin`     | специализации, врачи, пациенты               |
| `doctor`  | `/doctor`    | расписание, приемы, чат                      |
| `patient` | `/patient`   | профиль, запись к врачу, записи, чат         |

```powershell
cd src/Web.Generated
npm install
npm run dev
```

UI: http://localhost:3000 (порт в `vite.config.ts`, совпадает с `redirectUris` / `webOrigins` в realm).

Прокси Vite:

- `/api/chats` → CommunicationService `:5080`
- `/api` → AppointmentService `:5067`

Нужны запущенные Keycloak, AppointmentService и (для чата) CommunicationService.

### Тестовые пользователи

Из realm-export / seed (Development):

| Логин                         | Пароль          | Роль    |
|-------------------------------|-----------------|---------|
| `admin1`                      | `Admin1Pass!`   | admin   |
| `admin2`                      | `Admin2Pass!`   | admin   |
| `doctor1@medconnect.local`    | `Doctor1Pass!`  | doctor  |
| `doctor2@medconnect.local`    | `Doctor2Pass!`  | doctor  |
| `patient1@medconnect.local`   | `Patient1Pass!` | patient |
| `patient2@medconnect.local`   | `Patient2Pass!` | patient |

Врачи и пациенты создаются seed-ом AppointmentService при `Seed:DemoUsers=true` (первый успешный старт с Keycloak).

## 5. Логирование

- **Console** — при `dotnet run` (формат и уровни в `appsettings*.json`).
- **Seq** — только в Development (`http://localhost:5341`).
- В логах: `ServiceName`, `EnvironmentName`, `CorrelationId`, после JWT — `UserId`.

Заголовок запроса/ответа: `X-Correlation-ID`.

## 6. Тесты

```powershell
cd tests/AppointmentService.UnitTests
dotnet test
```

## 7. Чеклист для нового ПК

1. [ ] Установлены Docker, .NET 10 и Node.js 24
2. [ ] `dotnet dev-certs https --trust` — сертификат **trusted** (`--check --trust` ок)
3. [ ] `docker compose up -d`
4. [ ] Keycloak доступен на :8080, realm `medconnect`
5. [ ] Скопирован Client secret `medconnect-admin-cli`
6. [ ] `dotnet user-secrets set "Keycloak:AdminClientSecret" "..."` в AppointmentService
7. [ ] `dotnet run` в `AppointmentService`
8. [ ] `dotnet run` в `CommunicationService`
9. [ ] `npm install` + `npm run dev` в `src/Web.Generated`
10. [ ] (опционально) Seq UI на :5341, RabbitMQ UI на :15672
