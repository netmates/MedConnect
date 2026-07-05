# Swagger / OpenAPI Documentation

# MedConnect API

Версия: 1.0
Статус: Draft
Назначение: документация REST API для курсового проекта MedConnect
Сервисы: API Gateway / BFF, Appointment Service, Communication Service
Формат: OpenAPI 3.0 / Swagger UI

---

# 1. Назначение Swagger-документации

Swagger/OpenAPI-документация нужна для описания публичных HTTP API проекта MedConnect.

Она используется для:

1. Документирования endpoint’ов.
2. Демонстрации API на защите.
3. Тестирования запросов без frontend.
4. Фиксации контрактов между frontend и backend.
5. Генерации клиентского кода при необходимости.
6. Проверки моделей request/response.
7. Документирования кодов ошибок.
8. Демонстрации авторизации через JWT Bearer token.
9. Демонстрации зрелости проекта и инженерного подхода.

---

# 2. Общий подход

В проекте Swagger должен быть включён минимум в двух сервисах:

1. Appointment Service.
2. Communication Service.

Также можно добавить Swagger в API Gateway / BFF, если он будет предоставлять агрегированное публичное API для frontend.

Notification Service является Worker Service и не обязан иметь Swagger, так как он не предоставляет публичное REST API. Для него достаточно health check endpoint’ов и логов обработки событий.

---

# 3. Swagger-документы по сервисам

## 3.1. Appointment Service API

Назначение:

* врачи;
* специализации;
* расписание;
* слоты;
* записи к врачу.

Swagger URL локально:

```text id="sqbzd8"
/swagger/index.html
/openapi/v1.json
```

Базовый URL:

```text id="p2zzr2"
http://localhost:5001
```

---

## 3.2. Communication Service API

Назначение:

* чаты;
* сообщения;
* история сообщений.

Swagger URL локально:

```text id="wy1gu4"
/swagger/index.html
/openapi/v1.json
```

Базовый URL:

```text id="gftax7"
http://localhost:5002
```

SignalR Hub полностью через Swagger не описывается, потому что OpenAPI в первую очередь предназначен для HTTP API. В Swagger можно описать REST endpoints для истории сообщений, а SignalR-события вынести в отдельный раздел документации.

---

## 3.3. API Gateway / BFF API

Назначение:

* единая точка входа для frontend;
* маршрутизация к микросервисам;
* авторизация;
* агрегация данных.

Swagger URL локально:

```text id="c0jtto"
/swagger/index.html
/openapi/v1.json
```

Базовый URL:

```text id="1b3cyq"
http://localhost:5000
```

Если времени мало, API Gateway может не иметь собственной Swagger-документации, а просто проксировать запросы. Для защиты достаточно Swagger в Appointment Service и Communication Service.

---

# 4. Authentication

Все защищённые endpoints должны использовать JWT Bearer authentication.

Источник токена:

* Keycloak;
* либо временный Auth endpoint для MVP.

Заголовок авторизации:

```http id="1yugfq"
Authorization: Bearer <access_token>
```

В Swagger UI должна быть кнопка Authorize, куда можно вставить JWT-токен.

---

# 5. Общие HTTP-коды

Все сервисы должны использовать единый стиль ответов.

| Код                       | Значение                                    |
| ------------------------- | ------------------------------------------- |
| 200 OK                    | Успешное получение данных                   |
| 201 Created               | Успешное создание ресурса                   |
| 204 No Content            | Успешное действие без тела ответа           |
| 400 Bad Request           | Ошибка валидации                            |
| 401 Unauthorized          | Пользователь не авторизован                 |
| 403 Forbidden             | Недостаточно прав                           |
| 404 Not Found             | Ресурс не найден                            |
| 409 Conflict              | Конфликт состояния, например слот уже занят |
| 500 Internal Server Error | Непредвиденная ошибка сервера               |

---

# 6. Общий формат ошибки

Для ошибок рекомендуется использовать `ProblemDetails`.

Пример ответа:

```json id="jjydve"
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
  "errors": {
    "slotId": [
      "SlotId is required."
    ],
    "reason": [
      "Reason must be less than 500 characters."
    ]
  }
}
```

---

# 7. Appointment Service API

## 7.1. Tags

Swagger должен группировать endpoints по тегам:

* Doctors;
* Specializations;
* Schedule;
* Appointments;
* Health.

---

# 8. Doctors API

## GET /api/doctors

Получить список врачей.

Доступ:

* Guest;
* Patient;
* Doctor;
* Admin.

Query parameters:

| Параметр         | Тип     | Обязательный | Описание                |
| ---------------- | ------- | ------------ | ----------------------- |
| specializationId | uuid    | Нет          | Фильтр по специализации |
| search           | string  | Нет          | Поиск по ФИО            |
| page             | integer | Нет          | Номер страницы          |
| pageSize         | integer | Нет          | Размер страницы         |

Response `200 OK`:

```json id="pa8fvw"
{
  "items": [
    {
      "id": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
      "fullName": "Иванов Иван Иванович",
      "description": "Терапевт, стаж 10 лет",
      "experienceYears": 10,
      "specializations": [
        {
          "id": "251a63d3-50ec-46a6-8b88-3b8a776e1b90",
          "name": "Терапевт"
        }
      ],
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

---

## GET /api/doctors/{doctorId}

Получить карточку врача.

Доступ:

* Guest;
* Patient;
* Doctor;
* Admin.

Path parameters:

| Параметр | Тип  | Описание |
| -------- | ---- | -------- |
| doctorId | uuid | ID врача |

Response `200 OK`:

```json id="pry5s9"
{
  "id": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
  "fullName": "Иванов Иван Иванович",
  "description": "Терапевт, стаж 10 лет",
  "experienceYears": 10,
  "specializations": [
    {
      "id": "251a63d3-50ec-46a6-8b88-3b8a776e1b90",
      "name": "Терапевт"
    }
  ],
  "isActive": true
}
```

Ошибки:

| Код | Причина        |
| --- | -------------- |
| 404 | Врач не найден |

---

## POST /api/doctors

Создать врача.

Доступ:

* Admin.

Request:

```json id="olfllb"
{
  "fullName": "Иванов Иван Иванович",
  "description": "Терапевт, стаж 10 лет",
  "experienceYears": 10,
  "specializationIds": [
    "251a63d3-50ec-46a6-8b88-3b8a776e1b90"
  ]
}
```

Response `201 Created`:

```json id="psmf93"
{
  "id": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
  "fullName": "Иванов Иван Иванович",
  "description": "Терапевт, стаж 10 лет",
  "experienceYears": 10,
  "isActive": true
}
```

Ошибки:

| Код | Причина                                  |
| --- | ---------------------------------------- |
| 400 | Ошибка валидации                         |
| 401 | Пользователь не авторизован              |
| 403 | Пользователь не является администратором |

---

# 9. Specializations API

## GET /api/specializations

Получить список специализаций.

Доступ:

* Guest;
* Patient;
* Doctor;
* Admin.

Response `200 OK`:

```json id="vf0mfy"
[
  {
    "id": "251a63d3-50ec-46a6-8b88-3b8a776e1b90",
    "name": "Терапевт"
  },
  {
    "id": "819a5d0e-25d5-4462-8a79-a42f6251f332",
    "name": "Кардиолог"
  }
]
```

---

## POST /api/specializations

Создать специализацию.

Доступ:

* Admin.

Request:

```json id="al64qx"
{
  "name": "Невролог"
}
```

Response `201 Created`:

```json id="i6cyc4"
{
  "id": "98f44d83-cadc-4e7e-a68a-cd2199167332",
  "name": "Невролог"
}
```

Ошибки:

| Код | Причина                      |
| --- | ---------------------------- |
| 400 | Ошибка валидации             |
| 409 | Специализация уже существует |

---

# 10. Schedule API

## GET /api/doctors/{doctorId}/slots

Получить доступные слоты врача.

Доступ:

* Guest;
* Patient;
* Doctor;
* Admin.

Path parameters:

| Параметр | Тип  | Описание |
| -------- | ---- | -------- |
| doctorId | uuid | ID врача |

Query parameters:

| Параметр      | Тип      | Обязательный | Описание                       |
| ------------- | -------- | ------------ | ------------------------------ |
| from          | datetime | Нет          | Начало периода                 |
| to            | datetime | Нет          | Конец периода                  |
| onlyAvailable | boolean  | Нет          | Вернуть только свободные слоты |

Response `200 OK`:

```json id="vdc2rl"
[
  {
    "id": "47f652e3-f6a1-4597-9f63-105394821e53",
    "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
    "startTime": "2026-07-10T10:00:00Z",
    "endTime": "2026-07-10T10:30:00Z",
    "status": "Available"
  }
]
```

---

## POST /api/doctors/{doctorId}/slots

Создать слот врача.

Доступ:

* Doctor;
* Admin.

Request:

```json id="a65gc9"
{
  "startTime": "2026-07-10T10:00:00Z",
  "endTime": "2026-07-10T10:30:00Z"
}
```

Response `201 Created`:

```json id="syhtje"
{
  "id": "47f652e3-f6a1-4597-9f63-105394821e53",
  "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
  "startTime": "2026-07-10T10:00:00Z",
  "endTime": "2026-07-10T10:30:00Z",
  "status": "Available"
}
```

Ошибки:

| Код | Причина                                  |
| --- | ---------------------------------------- |
| 400 | Ошибка валидации                         |
| 403 | Врач пытается создать слот другому врачу |
| 409 | Слот пересекается с существующим слотом  |

---

## DELETE /api/slots/{slotId}

Отменить свободный слот.

Доступ:

* Doctor;
* Admin.

Response:

```http id="r6xjiz"
204 No Content
```

Ошибки:

| Код | Причина                                 |
| --- | --------------------------------------- |
| 404 | Слот не найден                          |
| 409 | Нельзя удалить уже забронированный слот |

---

# 11. Appointments API

## POST /api/appointments

Создать запись к врачу.

Доступ:

* Patient.

Request:

```json id="nbyf6w"
{
  "slotId": "47f652e3-f6a1-4597-9f63-105394821e53",
  "reason": "Консультация по результатам анализов"
}
```

Response `201 Created`:

```json id="ekcm0b"
{
  "id": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
  "patientId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
  "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
  "slotId": "47f652e3-f6a1-4597-9f63-105394821e53",
  "reason": "Консультация по результатам анализов",
  "status": "Created",
  "createdAt": "2026-07-03T12:00:00Z"
}
```

Ошибки:

| Код | Причина                             |
| --- | ----------------------------------- |
| 400 | Ошибка валидации                    |
| 401 | Пользователь не авторизован         |
| 403 | Только пациент может создать запись |
| 404 | Слот не найден                      |
| 409 | Слот уже занят                      |

---

## GET /api/appointments/my

Получить мои записи.

Доступ:

* Patient;
* Doctor.

Query parameters:

| Параметр | Тип      | Обязательный | Описание          |
| -------- | -------- | ------------ | ----------------- |
| status   | string   | Нет          | Фильтр по статусу |
| from     | datetime | Нет          | Начало периода    |
| to       | datetime | Нет          | Конец периода     |

Response `200 OK`:

```json id="fx55xs"
[
  {
    "id": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
    "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
    "doctorFullName": "Иванов Иван Иванович",
    "patientId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
    "slotId": "47f652e3-f6a1-4597-9f63-105394821e53",
    "startTime": "2026-07-10T10:00:00Z",
    "endTime": "2026-07-10T10:30:00Z",
    "status": "Created"
  }
]
```

---

## POST /api/appointments/{appointmentId}/cancel

Отменить запись.

Доступ:

* Patient;
* Doctor;
* Admin.

Request:

```json id="swi4r0"
{
  "reason": "Пользователь отменил запись"
}
```

Response:

```http id="s7dt84"
204 No Content
```

Ошибки:

| Код | Причина                                |
| --- | -------------------------------------- |
| 403 | Пользователь не имеет доступа к записи |
| 404 | Запись не найдена                      |
| 409 | Запись уже отменена или завершена      |

---

# 12. Communication Service API

## 12.1. Tags

Swagger должен группировать endpoints по тегам:

* Chats;
* Messages;
* Health.

---

# 13. Chats API

## GET /api/chats/my

Получить мои чаты.

Доступ:

* Patient;
* Doctor.

Response `200 OK`:

```json id="pjwo4v"
[
  {
    "id": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef",
    "appointmentId": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
    "patientId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
    "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
    "lastMessageText": "Здравствуйте, хотел уточнить детали приёма",
    "lastMessageCreatedAt": "2026-07-03T12:15:00Z"
  }
]
```

---

## GET /api/chats/{chatId}/messages

Получить историю сообщений в чате.

Доступ:

* Patient;
* Doctor.

Path parameters:

| Параметр | Тип  | Описание |
| -------- | ---- | -------- |
| chatId   | uuid | ID чата  |

Query parameters:

| Параметр | Тип      | Обязательный | Описание                             |
| -------- | -------- | ------------ | ------------------------------------ |
| limit    | integer  | Нет          | Количество сообщений                 |
| before   | datetime | Нет          | Получить сообщения до указанной даты |

Response `200 OK`:

```json id="fww8w2"
[
  {
    "id": "7b110ae5-014b-4908-a2e5-8f6e64d88430",
    "chatId": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef",
    "senderId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
    "senderRole": "Patient",
    "text": "Здравствуйте, хотел уточнить детали приёма",
    "createdAt": "2026-07-03T12:15:00Z"
  }
]
```

Ошибки:

| Код | Причина                                  |
| --- | ---------------------------------------- |
| 403 | Пользователь не является участником чата |
| 404 | Чат не найден                            |

---

## POST /api/chats/{chatId}/messages

Отправить сообщение через REST API.

Этот endpoint нужен как fallback и для тестирования через Swagger. Основная real-time отправка в UI может идти через SignalR.

Доступ:

* Patient;
* Doctor.

Request:

```json id="mbcngm"
{
  "text": "Здравствуйте, хотел уточнить детали приёма"
}
```

Response `201 Created`:

```json id="ba6jgx"
{
  "id": "7b110ae5-014b-4908-a2e5-8f6e64d88430",
  "chatId": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef",
  "senderId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
  "senderRole": "Patient",
  "text": "Здравствуйте, хотел уточнить детали приёма",
  "createdAt": "2026-07-03T12:15:00Z"
}
```

Ошибки:

| Код | Причина                                  |
| --- | ---------------------------------------- |
| 400 | Текст пустой или слишком длинный         |
| 403 | Пользователь не является участником чата |
| 404 | Чат не найден                            |

---

# 14. SignalR ChatHub Documentation

SignalR не описывается полноценно через Swagger, поэтому события нужно описать отдельно.

Hub endpoint:

```text id="0j5dq4"
/ws/chat
```

## Client to Server events

### JoinChat

Подключиться к комнате чата.

Payload:

```json id="uk8fql"
{
  "chatId": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef"
}
```

---

### LeaveChat

Выйти из комнаты чата.

Payload:

```json id="zgsfqh"
{
  "chatId": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef"
}
```

---

### SendMessage

Отправить сообщение.

Payload:

```json id="hiaytk"
{
  "chatId": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef",
  "text": "Здравствуйте, хотел уточнить детали приёма"
}
```

---

## Server to Client events

### ReceiveMessage

Получить новое сообщение.

Payload:

```json id="y9sm4f"
{
  "id": "7b110ae5-014b-4908-a2e5-8f6e64d88430",
  "chatId": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef",
  "senderId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
  "senderRole": "Patient",
  "text": "Здравствуйте, хотел уточнить детали приёма",
  "createdAt": "2026-07-03T12:15:00Z"
}
```

---

# 15. Health Checks

Каждый HTTP-сервис должен иметь health endpoint.

## GET /health

Проверка состояния сервиса.

Response `200 OK`:

```json id="jgbrn5"
{
  "status": "Healthy",
  "service": "Appointment Service"
}
```

## GET /health/ready

Проверка готовности сервиса к работе.

Должна проверять:

* подключение к PostgreSQL или MongoDB;
* подключение к Redis;
* подключение к RabbitMQ.

## GET /health/live

Проверка, что процесс жив.

---

# 16. Минимальный OpenAPI YAML для Appointment Service

```yaml id="bt49hm"
openapi: 3.0.4
info:
  title: MedConnect Appointment Service API
  version: v1
  description: API для врачей, специализаций, расписания и записей к врачу.
servers:
  - url: http://localhost:5001
    description: Local Appointment Service
tags:
  - name: Doctors
  - name: Specializations
  - name: Schedule
  - name: Appointments
  - name: Health

security:
  - bearerAuth: []

paths:
  /api/doctors:
    get:
      tags:
        - Doctors
      summary: Получить список врачей
      security: []
      parameters:
        - name: specializationId
          in: query
          required: false
          schema:
            type: string
            format: uuid
        - name: search
          in: query
          required: false
          schema:
            type: string
        - name: page
          in: query
          required: false
          schema:
            type: integer
            default: 1
        - name: pageSize
          in: query
          required: false
          schema:
            type: integer
            default: 20
      responses:
        "200":
          description: Список врачей
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/PagedDoctorResponse"
    post:
      tags:
        - Doctors
      summary: Создать врача
      description: Доступно только администратору.
      responses:
        "201":
          description: Врач создан
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/DoctorResponse"
        "400":
          description: Ошибка валидации
        "403":
          description: Недостаточно прав

  /api/doctors/{doctorId}:
    get:
      tags:
        - Doctors
      summary: Получить карточку врача
      security: []
      parameters:
        - name: doctorId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "200":
          description: Карточка врача
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/DoctorResponse"
        "404":
          description: Врач не найден

  /api/specializations:
    get:
      tags:
        - Specializations
      summary: Получить список специализаций
      security: []
      responses:
        "200":
          description: Список специализаций
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: "#/components/schemas/SpecializationResponse"
    post:
      tags:
        - Specializations
      summary: Создать специализацию
      responses:
        "201":
          description: Специализация создана
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/SpecializationResponse"
        "400":
          description: Ошибка валидации
        "409":
          description: Специализация уже существует

  /api/doctors/{doctorId}/slots:
    get:
      tags:
        - Schedule
      summary: Получить слоты врача
      security: []
      parameters:
        - name: doctorId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: from
          in: query
          required: false
          schema:
            type: string
            format: date-time
        - name: to
          in: query
          required: false
          schema:
            type: string
            format: date-time
        - name: onlyAvailable
          in: query
          required: false
          schema:
            type: boolean
      responses:
        "200":
          description: Список слотов
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: "#/components/schemas/ScheduleSlotResponse"
    post:
      tags:
        - Schedule
      summary: Создать слот врача
      responses:
        "201":
          description: Слот создан
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/ScheduleSlotResponse"
        "400":
          description: Ошибка валидации
        "409":
          description: Слот пересекается с существующим

  /api/slots/{slotId}:
    delete:
      tags:
        - Schedule
      summary: Удалить свободный слот
      parameters:
        - name: slotId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "204":
          description: Слот удалён
        "404":
          description: Слот не найден
        "409":
          description: Нельзя удалить занятый слот

  /api/appointments:
    post:
      tags:
        - Appointments
      summary: Создать запись к врачу
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/CreateAppointmentRequest"
      responses:
        "201":
          description: Запись создана
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/AppointmentResponse"
        "400":
          description: Ошибка валидации
        "404":
          description: Слот не найден
        "409":
          description: Слот уже занят

  /api/appointments/my:
    get:
      tags:
        - Appointments
      summary: Получить мои записи
      parameters:
        - name: status
          in: query
          required: false
          schema:
            type: string
            enum:
              - Created
              - Confirmed
              - Cancelled
              - Completed
        - name: from
          in: query
          required: false
          schema:
            type: string
            format: date-time
        - name: to
          in: query
          required: false
          schema:
            type: string
            format: date-time
      responses:
        "200":
          description: Список записей пользователя
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: "#/components/schemas/AppointmentListItemResponse"

  /api/appointments/{appointmentId}/cancel:
    post:
      tags:
        - Appointments
      summary: Отменить запись
      parameters:
        - name: appointmentId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        "204":
          description: Запись отменена
        "403":
          description: Нет доступа к записи
        "404":
          description: Запись не найдена
        "409":
          description: Запись уже отменена или завершена

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT

  schemas:
    SpecializationResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        name:
          type: string

    DoctorResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        fullName:
          type: string
        description:
          type: string
        experienceYears:
          type: integer
        specializations:
          type: array
          items:
            $ref: "#/components/schemas/SpecializationResponse"
        isActive:
          type: boolean

    PagedDoctorResponse:
      type: object
      properties:
        items:
          type: array
          items:
            $ref: "#/components/schemas/DoctorResponse"
        page:
          type: integer
        pageSize:
          type: integer
        totalCount:
          type: integer

    ScheduleSlotResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        doctorId:
          type: string
          format: uuid
        startTime:
          type: string
          format: date-time
        endTime:
          type: string
          format: date-time
        status:
          type: string
          enum:
            - Available
            - Booked
            - Cancelled

    CreateAppointmentRequest:
      type: object
      required:
        - slotId
      properties:
        slotId:
          type: string
          format: uuid
        reason:
          type: string
          maxLength: 500

    AppointmentResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        patientId:
          type: string
          format: uuid
        doctorId:
          type: string
          format: uuid
        slotId:
          type: string
          format: uuid
        reason:
          type: string
        status:
          type: string
          enum:
            - Created
            - Confirmed
            - Cancelled
            - Completed
        createdAt:
          type: string
          format: date-time

    AppointmentListItemResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        doctorId:
          type: string
          format: uuid
        doctorFullName:
          type: string
        patientId:
          type: string
          format: uuid
        slotId:
          type: string
          format: uuid
        startTime:
          type: string
          format: date-time
        endTime:
          type: string
          format: date-time
        status:
          type: string
```

---

# 17. Минимальный OpenAPI YAML для Communication Service

```yaml id="ewx8mn"
openapi: 3.0.4
info:
  title: MedConnect Communication Service API
  version: v1
  description: API для чатов, истории сообщений и fallback-отправки сообщений.
servers:
  - url: http://localhost:5002
    description: Local Communication Service
tags:
  - name: Chats
  - name: Messages
  - name: Health

security:
  - bearerAuth: []

paths:
  /api/chats/my:
    get:
      tags:
        - Chats
      summary: Получить мои чаты
      responses:
        "200":
          description: Список чатов пользователя
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: "#/components/schemas/ChatListItemResponse"
        "401":
          description: Пользователь не авторизован

  /api/chats/{chatId}/messages:
    get:
      tags:
        - Messages
      summary: Получить историю сообщений в чате
      parameters:
        - name: chatId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: limit
          in: query
          required: false
          schema:
            type: integer
            default: 50
        - name: before
          in: query
          required: false
          schema:
            type: string
            format: date-time
      responses:
        "200":
          description: История сообщений
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: "#/components/schemas/MessageResponse"
        "403":
          description: Пользователь не является участником чата
        "404":
          description: Чат не найден

    post:
      tags:
        - Messages
      summary: Отправить сообщение через REST API
      description: Fallback endpoint для отправки сообщений и тестирования через Swagger. Основной UI может использовать SignalR.
      parameters:
        - name: chatId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/SendMessageRequest"
      responses:
        "201":
          description: Сообщение создано
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/MessageResponse"
        "400":
          description: Ошибка валидации
        "403":
          description: Пользователь не является участником чата
        "404":
          description: Чат не найден

  /health:
    get:
      tags:
        - Health
      summary: Health check
      security: []
      responses:
        "200":
          description: Сервис работает

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT

  schemas:
    ChatListItemResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        appointmentId:
          type: string
          format: uuid
        patientId:
          type: string
          format: uuid
        doctorId:
          type: string
          format: uuid
        lastMessageText:
          type: string
        lastMessageCreatedAt:
          type: string
          format: date-time

    SendMessageRequest:
      type: object
      required:
        - text
      properties:
        text:
          type: string
          minLength: 1
          maxLength: 2000

    MessageResponse:
      type: object
      properties:
        id:
          type: string
          format: uuid
        chatId:
          type: string
          format: uuid
        senderId:
          type: string
          format: uuid
        senderRole:
          type: string
          enum:
            - Patient
            - Doctor
        text:
          type: string
        createdAt:
          type: string
          format: date-time
```

---

# 18. Что пригодится для реализации Swagger в ASP.NET Core

## 18.1. Пакеты

Для ASP.NET Core 10 можно использовать встроенный OpenAPI:

```bash id="rwfk35"
dotnet add package Microsoft.AspNetCore.OpenApi
```

Для Swagger UI:

```bash id="ur8o7e"
dotnet add package Swashbuckle.AspNetCore.SwaggerUi
```

Если использовать классический Swashbuckle-подход:

```bash id="qh6rsb"
dotnet add package Swashbuckle.AspNetCore
```

---

## 18.2. Минимальная настройка Program.cs

Вариант со встроенной генерацией OpenAPI и Swagger UI:

```csharp id="fedzmg"
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "MedConnect API v1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## 18.3. Что добавить в контроллеры

Для хорошей Swagger-документации нужны атрибуты:

```csharp id="oy2acg"
[ApiController]
[Route("api/doctors")]
[Produces("application/json")]
public sealed class DoctorsController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<DoctorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors(
        [FromQuery] Guid? specializationId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // implementation
    }

    [HttpGet("{doctorId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoctorById(
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        // implementation
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateDoctor(
        [FromBody] CreateDoctorRequest request,
        CancellationToken cancellationToken = default)
    {
        // implementation
    }
}
```

---

## 18.4. XML comments

Желательно включить XML comments, чтобы Swagger показывал описания endpoint’ов, DTO и полей.

В `.csproj`:

```xml id="b83crj"
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

Пример DTO:

```csharp id="nkw4h7"
/// <summary>
/// Запрос на создание записи к врачу.
/// </summary>
public sealed class CreateAppointmentRequest
{
    /// <summary>
    /// ID свободного слота врача.
    /// </summary>
    public Guid SlotId { get; init; }

    /// <summary>
    /// Причина обращения пациента.
    /// </summary>
    public string? Reason { get; init; }
}
```

---

## 18.5. DTO, которые понадобятся

### Appointment Service

Request DTO:

* CreateDoctorRequest;
* CreateSpecializationRequest;
* CreateScheduleSlotRequest;
* CreateAppointmentRequest;
* CancelAppointmentRequest.

Response DTO:

* DoctorResponse;
* SpecializationResponse;
* ScheduleSlotResponse;
* AppointmentResponse;
* AppointmentListItemResponse;
* PagedResponse<T>.

---

### Communication Service

Request DTO:

* SendMessageRequest;
* JoinChatRequest — для SignalR-документации;
* LeaveChatRequest — для SignalR-документации.

Response DTO:

* ChatListItemResponse;
* MessageResponse.

---

## 18.6. FluentValidation

Swagger сам по себе не заменяет FluentValidation.

FluentValidation нужен для реальной проверки данных:

* пустой `SlotId`;
* слишком длинный `Reason`;
* пустой текст сообщения;
* пересечение слотов;
* попытка записаться на занятый слот.

Swagger показывает контракт, а FluentValidation обеспечивает выполнение правил на backend.

---

## 18.7. ProblemDetails

Для единообразных ошибок нужно настроить `ProblemDetails`.

Пример ошибок, которые стоит возвращать:

* ValidationProblemDetails для 400;
* ProblemDetails для 404;
* ProblemDetails для 409;
* ProblemDetails для 500.

Это удобно показать на защите: можно специально отправить некорректный запрос из Swagger UI и показать красивую ошибку.

---

# 19. Что показать на защите через Swagger

## Сценарий 1. Получение врачей

1. Открыть Swagger Appointment Service.
2. Выполнить `GET /api/doctors`.
3. Показать список врачей.
4. Выполнить фильтр по специализации.

Что демонстрирует:

* REST API;
* Swagger-документацию;
* response DTO;
* фильтрацию;
* возможно Redis-кеширование.

---

## Сценарий 2. Создание слота врачом

1. Нажать Authorize.
2. Вставить JWT врача.
3. Выполнить `POST /api/doctors/{doctorId}/slots`.
4. Показать созданный слот.

Что демонстрирует:

* JWT Bearer auth;
* role-based access;
* FluentValidation;
* EF Core;
* PostgreSQL.

---

## Сценарий 3. Создание записи пациентом

1. Авторизоваться как Patient.
2. Выполнить `POST /api/appointments`.
3. Получить `201 Created`.
4. Показать, что слот стал занятым.
5. Показать событие в RabbitMQ.
6. Показать лог Notification Service.

Что демонстрирует:

* бизнес-сценарий;
* транзакционную логику;
* защиту от двойного бронирования;
* брокер сообщений;
* Notification Service.

---

## Сценарий 4. Ошибка двойного бронирования

1. Повторить `POST /api/appointments` на тот же `slotId`.
2. Получить `409 Conflict`.

Что демонстрирует:

* бизнес-инварианты;
* корректные HTTP-коды;
* обработку ошибок;
* ProblemDetails.

---

## Сценарий 5. История сообщений

1. Открыть Swagger Communication Service.
2. Авторизоваться как Patient или Doctor.
3. Выполнить `GET /api/chats/my`.
4. Выполнить `GET /api/chats/{chatId}/messages`.
5. Выполнить `POST /api/chats/{chatId}/messages`.

Что демонстрирует:

* MongoDB;
* REST fallback для сообщений;
* проверку доступа;
* публикацию события `MessageCreated`.

---

# 20. Что НЕ стоит документировать в Swagger

Swagger не стоит использовать для:

1. Внутреннего устройства микросервисов.
2. RabbitMQ-событий как HTTP endpoint’ов.
3. Полного описания SignalR-протокола.
4. gRPC-контрактов.
5. Kubernetes-манифестов.
6. Метрик Prometheus.

Для этого лучше использовать отдельные документы:

* AsyncAPI — для событий RabbitMQ;
* `.proto` файлы — для gRPC;
* README — для запуска проекта;
* C4 diagrams — для архитектуры;
* Grafana dashboards — для мониторинга.

---

# 21. Минимальный чеклист готовности Swagger

Для защиты достаточно, если выполнено:

1. Swagger UI открывается в Appointment Service.
2. Swagger UI открывается в Communication Service.
3. Все endpoint’ы сгруппированы по тегам.
4. Есть описание request/response DTO.
5. Есть JWT Authorize button.
6. Защищённые endpoint’ы требуют токен.
7. Ошибки возвращаются в едином формате.
8. У endpoint’ов указаны HTTP-коды.
9. Есть health endpoint.
10. Через Swagger можно пройти сценарий создания записи.
11. Через Swagger можно показать ошибку `409 Conflict`.
12. Через Swagger можно отправить сообщение через REST fallback.

---

# 22. Рекомендация для MVP

Для курсового проекта не нужно делать идеальную enterprise-документацию.

Достаточно сделать Swagger так, чтобы через него можно было показать:

1. список врачей;
2. список слотов;
3. создание записи;
4. отмену записи;
5. список чатов;
6. историю сообщений;
7. отправку сообщения;
8. авторизацию через JWT;
9. ошибки валидации;
10. ошибку двойного бронирования.

Это будет хорошо смотреться на защите, потому что Swagger покажет не только API, но и зрелость backend-подхода: контракты, DTO, безопасность, ошибки и понятные сценарии тестирования.
