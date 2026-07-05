# RabbitMQ / Integration Events Documentation

# MedConnect

Версия: 1.0
Статус: Draft
Назначение: документация событийного взаимодействия микросервисов через RabbitMQ
Брокер: RabbitMQ
Протокол: AMQP
Формат сообщений: JSON
Основной потребитель событий: Notification Service

---

# 1. Назначение RabbitMQ в проекте

RabbitMQ используется для асинхронного взаимодействия микросервисов MedConnect.

Основные задачи брокера:

1. Развязать сервисы между собой.
2. Не заставлять Appointment Service ждать обработку уведомлений.
3. Передавать бизнес-события между сервисами.
4. Повысить устойчивость системы к временной недоступности Notification Service.
5. Продемонстрировать event-driven подход в рамках курсового проекта.

В MVP RabbitMQ используется для передачи следующих событий:

1. `AppointmentCreated`;
2. `AppointmentCancelled`;
3. `MessageCreated`.

---

# 2. Общий принцип взаимодействия

В MedConnect есть сервисы-публикаторы и сервисы-потребители.

## Publishers

Публикаторы отправляют события в RabbitMQ.

| Сервис                | События                                  |
| --------------------- | ---------------------------------------- |
| Appointment Service   | AppointmentCreated, AppointmentCancelled |
| Communication Service | MessageCreated                           |

## Consumers

Потребители читают события из RabbitMQ.

| Сервис               | Назначение                                  |
| -------------------- | ------------------------------------------- |
| Notification Service | Обрабатывает события и логирует уведомления |

В MVP Notification Service не отправляет реальные email/SMS. Вместо этого он логирует факт уведомления.

---

# 3. RabbitMQ Topology

## 3.1. Exchanges

Для MVP используется один основной topic exchange:

```text
medconnect.events
```

Тип exchange:

```text
topic
```

Назначение:

* принимать все integration events;
* маршрутизировать события по routing key;
* позволять добавлять новых consumers без изменения publishers.

---

## 3.2. Dead Letter Exchange

Для ошибочных сообщений используется отдельный dead-letter exchange:

```text
medconnect.dlx
```

Тип exchange:

```text
topic
```

Назначение:

* принимать сообщения, которые не удалось обработать;
* хранить ошибочные сообщения для анализа;
* не блокировать основную очередь Notification Service.

---

# 4. Routing Keys

Routing key должен отражать домен, сущность и событие.

Формат:

```text
{domain}.{entity}.{event}.v{version}
```

Примеры:

```text
appointments.appointment.created.v1
appointments.appointment.cancelled.v1
communication.message.created.v1
```

---

# 5. Queues

## 5.1. Основные очереди

| Queue                                | Consumer             | Routing key                           |
| ------------------------------------ | -------------------- | ------------------------------------- |
| notification.appointment-created.q   | Notification Service | appointments.appointment.created.v1   |
| notification.appointment-cancelled.q | Notification Service | appointments.appointment.cancelled.v1 |
| notification.message-created.q       | Notification Service | communication.message.created.v1      |

---

## 5.2. Dead-letter очереди

| Queue                                  | Назначение                            |
| -------------------------------------- | ------------------------------------- |
| notification.appointment-created.dlq   | Ошибки обработки AppointmentCreated   |
| notification.appointment-cancelled.dlq | Ошибки обработки AppointmentCancelled |
| notification.message-created.dlq       | Ошибки обработки MessageCreated       |

---

# 6. Схема топологии

```text
Appointment Service
        |
        | publish AppointmentCreated / AppointmentCancelled
        v
+----------------------+
| medconnect.events    |
| topic exchange       |
+----------------------+
        |
        | appointments.appointment.created.v1
        v
notification.appointment-created.q
        |
        v
Notification Service


Communication Service
        |
        | publish MessageCreated
        v
+----------------------+
| medconnect.events    |
| topic exchange       |
+----------------------+
        |
        | communication.message.created.v1
        v
notification.message-created.q
        |
        v
Notification Service
```

---

# 7. Общий формат сообщения

Все события должны иметь общий envelope.

```json
{
  "eventId": "3f6f72f7-3997-4d1a-9c0c-6f62cf57ec0d",
  "eventType": "AppointmentCreated",
  "eventVersion": 1,
  "occurredAt": "2026-07-03T12:00:00Z",
  "correlationId": "7a8b5a1d-0ad5-46d1-8c9f-6e9a6d5e4c21",
  "source": "AppointmentService",
  "payload": {}
}
```

## 7.1. Поля envelope

| Поле          | Тип         | Обязательное | Описание                    |
| ------------- | ----------- | ------------ | --------------------------- |
| eventId       | uuid        | Да           | Уникальный ID события       |
| eventType     | string      | Да           | Название события            |
| eventVersion  | integer     | Да           | Версия события              |
| occurredAt    | datetime    | Да           | Время возникновения события |
| correlationId | uuid/string | Да           | ID запроса для трассировки  |
| source        | string      | Да           | Сервис-источник события     |
| payload       | object      | Да           | Бизнес-данные события       |

---

# 8. Event Catalog

# 8.1. AppointmentCreated

## Назначение

Событие публикуется после успешного создания записи пациента к врачу.

## Publisher

```text
Appointment Service
```

## Consumer

```text
Notification Service
```

## Exchange

```text
medconnect.events
```

## Routing key

```text
appointments.appointment.created.v1
```

## Queue

```text
notification.appointment-created.q
```

## Payload

```json
{
  "appointmentId": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
  "patientId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
  "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
  "slotId": "47f652e3-f6a1-4597-9f63-105394821e53",
  "startTime": "2026-07-10T10:00:00Z",
  "endTime": "2026-07-10T10:30:00Z",
  "reason": "Консультация по результатам анализов"
}
```

## Полное сообщение

```json
{
  "eventId": "3f6f72f7-3997-4d1a-9c0c-6f62cf57ec0d",
  "eventType": "AppointmentCreated",
  "eventVersion": 1,
  "occurredAt": "2026-07-03T12:00:00Z",
  "correlationId": "7a8b5a1d-0ad5-46d1-8c9f-6e9a6d5e4c21",
  "source": "AppointmentService",
  "payload": {
    "appointmentId": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
    "patientId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
    "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
    "slotId": "47f652e3-f6a1-4597-9f63-105394821e53",
    "startTime": "2026-07-10T10:00:00Z",
    "endTime": "2026-07-10T10:30:00Z",
    "reason": "Консультация по результатам анализов"
  }
}
```

## Действие Notification Service

Notification Service должен:

1. Получить сообщение.
2. Проверить `eventId`.
3. Залогировать уведомление пациенту.
4. Залогировать уведомление врачу.
5. Подтвердить обработку сообщения через ACK.

Пример лога:

```json
{
  "level": "Information",
  "message": "Appointment notification was processed",
  "eventType": "AppointmentCreated",
  "appointmentId": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
  "patientId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
  "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
  "correlationId": "7a8b5a1d-0ad5-46d1-8c9f-6e9a6d5e4c21"
}
```

---

# 8.2. AppointmentCancelled

## Назначение

Событие публикуется после отмены записи.

## Publisher

```text
Appointment Service
```

## Consumer

```text
Notification Service
```

## Exchange

```text
medconnect.events
```

## Routing key

```text
appointments.appointment.cancelled.v1
```

## Queue

```text
notification.appointment-cancelled.q
```

## Payload

```json
{
  "appointmentId": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
  "patientId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
  "doctorId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
  "slotId": "47f652e3-f6a1-4597-9f63-105394821e53",
  "cancelledByUserId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
  "cancelledByRole": "Patient",
  "cancelReason": "Пользователь отменил запись",
  "cancelledAt": "2026-07-03T13:00:00Z"
}
```

## Полное сообщение

```json
{
  "eventId": "0eaed0c5-ef13-4647-9b4c-080e9408cf23",
  "eventType": "AppointmentCancelled",
  "eventVersion": 1,
  "occurredAt": "2026-07-03T13:00:00Z",
  "correlationId": "91ce8d1e-f61c-4861-a2a2-0845cc864a83",
  "source": "AppointmentService",
  "payload": {
    "appointmentId": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
    "patientId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
    "doctorId": "e1f6b706-95b2-4bdb-9ed2-4a7fd3ab9c11",
    "slotId": "47f652e3-f6a1-4597-9f63-105394821e53",
    "cancelledByUserId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
    "cancelledByRole": "Patient",
    "cancelReason": "Пользователь отменил запись",
    "cancelledAt": "2026-07-03T13:00:00Z"
  }
}
```

## Действие Notification Service

Notification Service должен:

1. Получить сообщение.
2. Залогировать уведомление об отмене.
3. Подтвердить обработку через ACK.

---

# 8.3. MessageCreated

## Назначение

Событие публикуется после отправки сообщения в чате.

## Publisher

```text
Communication Service
```

## Consumer

```text
Notification Service
```

## Exchange

```text
medconnect.events
```

## Routing key

```text
communication.message.created.v1
```

## Queue

```text
notification.message-created.q
```

## Payload

```json
{
  "messageId": "7b110ae5-014b-4908-a2e5-8f6e64d88430",
  "chatId": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef",
  "appointmentId": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
  "senderId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
  "senderRole": "Patient",
  "recipientId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
  "recipientRole": "Doctor",
  "textPreview": "Здравствуйте, хотел уточнить детали приёма",
  "createdAt": "2026-07-03T12:15:00Z"
}
```

## Полное сообщение

```json
{
  "eventId": "ca172bb1-742b-4f5b-b0eb-14917175e40c",
  "eventType": "MessageCreated",
  "eventVersion": 1,
  "occurredAt": "2026-07-03T12:15:00Z",
  "correlationId": "ab148f36-774c-4755-b015-f582fd7e9a6d",
  "source": "CommunicationService",
  "payload": {
    "messageId": "7b110ae5-014b-4908-a2e5-8f6e64d88430",
    "chatId": "5c6678e1-4a4a-4ef0-b19a-e89338df36ef",
    "appointmentId": "76e32dc5-f246-4a7b-bd86-4e28216088dd",
    "senderId": "971f8c50-c4e2-40a9-ad73-d524b40b5a3d",
    "senderRole": "Patient",
    "recipientId": "e1f6b706-95b2-4bdb-9ed2-4b7fd3ab9c11",
    "recipientRole": "Doctor",
    "textPreview": "Здравствуйте, хотел уточнить детали приёма",
    "createdAt": "2026-07-03T12:15:00Z"
  }
}
```

## Действие Notification Service

Notification Service должен:

1. Получить событие.
2. Залогировать уведомление получателю сообщения.
3. Подтвердить обработку через ACK.

---

# 9. Правила публикации событий

## 9.1. Appointment Service

Appointment Service публикует событие только после успешного изменения состояния в PostgreSQL.

Пример:

1. Пациент создаёт запись.
2. Appointment Service открывает транзакцию.
3. Проверяет, что слот свободен.
4. Создаёт appointment.
5. Меняет статус slot на `Booked`.
6. Сохраняет изменения.
7. Публикует `AppointmentCreated`.

Для более надёжной реализации можно использовать Transactional Outbox.

---

## 9.2. Communication Service

Communication Service публикует `MessageCreated` только после успешного сохранения сообщения в MongoDB.

Пример:

1. Пользователь отправляет сообщение.
2. Communication Service проверяет доступ к appointment через gRPC.
3. Сохраняет сообщение в MongoDB.
4. Доставляет сообщение через SignalR.
5. Публикует `MessageCreated`.

---

# 10. Правила обработки сообщений

Notification Service должен использовать manual acknowledgement.

## Успешная обработка

Если сообщение обработано успешно:

```text
ACK
```

## Ошибка обработки

Если произошла временная ошибка:

```text
NACK / retry
```

## Невалидное сообщение

Если сообщение невозможно обработать:

```text
Reject without requeue
```

После превышения лимита retry сообщение должно попадать в DLQ.

---

# 11. Retry Policy

Для MVP достаточно простой retry-политики.

| Попытка | Задержка  |
| ------- | --------- |
| 1       | сразу     |
| 2       | 5 секунд  |
| 3       | 15 секунд |
| 4       | 30 секунд |

После 4 неуспешных попыток сообщение отправляется в dead-letter queue.

---

# 12. Dead Letter Queue

DLQ нужна для сообщений, которые не удалось обработать.

Причины попадания в DLQ:

1. Невалидный JSON.
2. Неизвестный `eventType`.
3. Невалидная версия события.
4. Ошибка десериализации.
5. Превышен лимит повторной обработки.
6. Внутренняя ошибка Notification Service.

Для каждого основного consumer queue есть отдельная DLQ:

```text
notification.appointment-created.dlq
notification.appointment-cancelled.dlq
notification.message-created.dlq
```

---

# 13. Idempotency

Consumer должен быть идемпотентным.

Это значит, что повторная обработка одного и того же события не должна приводить к некорректному результату.

Для этого Notification Service может хранить обработанные `eventId`.

В MVP можно сделать упрощённо:

1. Логировать `eventId`.
2. Не хранить историю обработанных событий.
3. На защите объяснить, что в production нужна таблица/коллекция `ProcessedMessages`.

Production-вариант:

```text
ProcessedMessages
- EventId
- EventType
- ProcessedAt
```

---

# 14. Message Versioning

Каждое событие должно иметь версию.

Пример:

```json
{
  "eventType": "AppointmentCreated",
  "eventVersion": 1
}
```

Routing key также содержит версию:

```text
appointments.appointment.created.v1
```

Если структура события изменится несовместимо, нужно добавить новую версию:

```text
appointments.appointment.created.v2
```

Старые consumers могут продолжать слушать `v1`, новые — `v2`.

---

# 15. CorrelationId

Каждое событие должно содержать `correlationId`.

Назначение:

1. Связать HTTP-запрос с событием в RabbitMQ.
2. Связать логи нескольких микросервисов.
3. Упростить отладку.
4. Показать distributed tracing на защите.

Пример цепочки:

```text
Frontend
  -> API Gateway
    -> Appointment Service
      -> RabbitMQ
        -> Notification Service
```

Один и тот же `correlationId` должен проходить через всю цепочку.

---

# 16. Рекомендуемые headers RabbitMQ

Кроме JSON payload, полезно передавать часть информации в headers.

| Header           | Пример                               | Назначение       |
| ---------------- | ------------------------------------ | ---------------- |
| x-correlation-id | 7a8b5a1d-0ad5-46d1-8c9f-6e9a6d5e4c21 | Трассировка      |
| x-event-id       | 3f6f72f7-3997-4d1a-9c0c-6f62cf57ec0d | ID события       |
| x-event-type     | AppointmentCreated                   | Тип события      |
| x-event-version  | 1                                    | Версия события   |
| x-source         | AppointmentService                   | Источник события |

---

# 17. Рекомендуемые настройки сообщений

Для MVP:

| Настройка          | Значение         |
| ------------------ | ---------------- |
| Exchange durable   | true             |
| Queue durable      | true             |
| Message persistent | true             |
| Manual ACK         | true             |
| Publisher confirms | желательно       |
| Prefetch count     | 10               |
| Content type       | application/json |
| Encoding           | UTF-8            |

---

# 18. Docker Compose

RabbitMQ можно поднять через Docker Compose.

```yaml
rabbitmq:
  image: rabbitmq:4-management
  container_name: medconnect-rabbitmq
  ports:
    - "5672:5672"
    - "15672:15672"
  environment:
    RABBITMQ_DEFAULT_USER: medconnect
    RABBITMQ_DEFAULT_PASS: medconnect
  volumes:
    - rabbitmq-data:/var/lib/rabbitmq

volumes:
  rabbitmq-data:
```

Management UI:

```text
http://localhost:15672
```

AMQP endpoint:

```text
amqp://medconnect:medconnect@localhost:5672
```

---

# 19. Пример конфигурации приложения

## Appointment Service

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "medconnect",
    "Password": "medconnect",
    "ExchangeName": "medconnect.events"
  }
}
```

## Communication Service

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "medconnect",
    "Password": "medconnect",
    "ExchangeName": "medconnect.events"
  }
}
```

## Notification Service

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "medconnect",
    "Password": "medconnect",
    "ExchangeName": "medconnect.events",
    "DeadLetterExchangeName": "medconnect.dlx"
  }
}
```

---

# 20. Что логировать

## Publisher logs

Публикатор должен логировать:

1. `eventId`;
2. `eventType`;
3. `correlationId`;
4. routing key;
5. exchange;
6. результат публикации.

Пример:

```json
{
  "level": "Information",
  "message": "Integration event was published",
  "eventId": "3f6f72f7-3997-4d1a-9c0c-6f62cf57ec0d",
  "eventType": "AppointmentCreated",
  "routingKey": "appointments.appointment.created.v1",
  "exchange": "medconnect.events",
  "correlationId": "7a8b5a1d-0ad5-46d1-8c9f-6e9a6d5e4c21"
}
```

## Consumer logs

Consumer должен логировать:

1. получение сообщения;
2. успешную обработку;
3. ошибку обработки;
4. отправку в DLQ;
5. `eventId`;
6. `correlationId`.

Пример:

```json
{
  "level": "Information",
  "message": "Integration event was consumed",
  "eventId": "3f6f72f7-3997-4d1a-9c0c-6f62cf57ec0d",
  "eventType": "AppointmentCreated",
  "consumer": "NotificationService",
  "correlationId": "7a8b5a1d-0ad5-46d1-8c9f-6e9a6d5e4c21"
}
```

---

# 21. Метрики RabbitMQ

Для защиты можно показать:

1. количество сообщений в очередях;
2. количество consumers;
3. скорость публикации сообщений;
4. скорость обработки сообщений;
5. количество сообщений в DLQ;
6. состояние соединений;
7. состояние каналов.

Минимально достаточно показать RabbitMQ Management UI.

Дополнительно можно подключить Prometheus + Grafana.

---

# 22. Acceptance Criteria

RabbitMQ-интеграция считается реализованной, если:

1. RabbitMQ запускается через Docker Compose.
2. Appointment Service публикует `AppointmentCreated`.
3. Appointment Service публикует `AppointmentCancelled`.
4. Communication Service публикует `MessageCreated`.
5. Notification Service читает все три события.
6. Notification Service пишет структурированные логи.
7. В RabbitMQ Management UI видны exchanges, queues и bindings.
8. При ошибке обработки сообщение попадает в DLQ.
9. Сообщения содержат `eventId`.
10. Сообщения содержат `correlationId`.
11. События имеют версию.
12. На защите можно показать end-to-end flow: создание записи → событие → очередь → Notification Service → лог.

---

# 23. Демонстрационный сценарий для защиты

## Сценарий 1. AppointmentCreated

1. Открыть Swagger Appointment Service.
2. Создать запись через `POST /api/appointments`.
3. Открыть RabbitMQ Management UI.
4. Показать exchange `medconnect.events`.
5. Показать очередь `notification.appointment-created.q`.
6. Показать, что Notification Service обработал сообщение.
7. Показать лог с `eventId` и `correlationId`.

---

## Сценарий 2. AppointmentCancelled

1. Через Swagger отменить запись.
2. Показать событие `AppointmentCancelled`.
3. Показать обработку в Notification Service.

---

## Сценарий 3. MessageCreated

1. Отправить сообщение в чате.
2. Показать событие `MessageCreated`.
3. Показать очередь `notification.message-created.q`.
4. Показать лог Notification Service.

---

## Сценарий 4. DLQ

1. Временно сломать consumer или отправить невалидное сообщение.
2. Показать ошибку обработки.
3. Показать попадание сообщения в DLQ.
4. Объяснить, что такие сообщения можно анализировать вручную.

---

# 24. Что можно упростить в MVP

Если времени мало, можно упростить:

1. Не делать полноценный retry с задержками.
2. Не хранить `ProcessedMessages`.
3. Не делать Transactional Outbox.
4. Не подключать Prometheus к RabbitMQ.
5. Не делать отдельные DLQ на каждый event type.

Но желательно оставить:

1. topic exchange;
2. routing keys;
3. durable queues;
4. manual ACK;
5. `eventId`;
6. `correlationId`;
7. structured logs;
8. хотя бы одну DLQ.

---

# 25. Рекомендация по библиотеке

Для ASP.NET Core можно использовать один из двух подходов.

## Вариант 1. RabbitMQ.Client

Плюсы:

* лучше понимаешь устройство RabbitMQ;
* видно exchanges, queues, channels, ACK/NACK;
* полезно для обучения.

Минусы:

* больше ручного кода;
* нужно самому реализовывать retry, reconnect, serialization, DLQ.

## Вариант 2. MassTransit

Плюсы:

* меньше инфраструктурного кода;
* есть готовые retry policies;
* проще consumers;
* удобно для production-like подхода.

Минусы:

* часть деталей RabbitMQ скрыта библиотекой;
* для обучения брокеру может быть менее прозрачно.

Для курсовой можно выбрать так:

* если цель — глубже понять RabbitMQ, использовать `RabbitMQ.Client`;
* если цель — быстрее собрать рабочий проект, использовать `MassTransit`.

---

# 26. Итог

RabbitMQ в MedConnect закрывает важную инфраструктурную часть проекта:

1. брокер сообщений;
2. event-driven interaction;
3. асинхронные уведомления;
4. слабая связанность микросервисов;
5. retry/DLQ;
6. correlationId;
7. structured logging;
8. демонстрация событий на защите.

Главный сценарий для демонстрации:

```text
Пациент создаёт запись
  -> Appointment Service сохраняет appointment
  -> Appointment Service публикует AppointmentCreated
  -> RabbitMQ маршрутизирует событие
  -> Notification Service читает сообщение
  -> Notification Service логирует уведомление
```
