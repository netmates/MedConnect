# ADR 0001. Три очереди Notification Service

## Статус

Принято

## Контекст

Notification Service читает три интеграционных события с topic exchange `medconnect.events`:

- `AppointmentCreated` — `appointments.appointment.created.v1`
- `AppointmentCancelled` — `appointments.appointment.cancelled.v1`
- `MessageCreated` — `communication.message.created.v1`

В RabbitMQ consumer подписывается на очередь. Одна очередь может иметь несколько bindings и принять все три routing key. Сценарий защиты и [RabbitMQ.md](../RabbitMQ.md) называют отдельные очереди `notification.appointment-created.q`, `notification.appointment-cancelled.q`, `notification.message-created.q` и отдельную dead-letter очередь на каждую.

Обработка у всех трёх событий одинаковая: проверить конверт, записать структурированный лог, подтвердить сообщение. Ошибка десериализации, пустой payload, чужой `eventType`, `eventVersion` отличная от 1 и сбой обработки завершаются одинаково.

## Решение

Notification Service объявляет три рабочие очереди и три DLQ. Dead-letter exchange — `medconnect.dlx` (direct). У рабочей очереди заданы `x-dead-letter-exchange` и `x-dead-letter-routing-key` на свою DLQ.

В коде один generic consumer, зарегистрированный тремя hosted service. Успех — ACK. Любая ошибка — NACK без requeue, и брокер перекладывает сообщение в DLQ этой очереди. Повторных публикаций и пауз внутри процесса нет.

## Рассмотренный вариант

Общая очередь `notification.events.q` с тремя bindings тоже корректна: один consumer, одна подписка, тот же контракт событий. Для текущего объёма отдельный SLA и отдельное масштабирование по типу события не нужны.

Отдельные очереди оставляем по трём причинам:

1. Ошибочное сообщение одного типа лежит в своей DLQ и не смешивается с остальными событиями.
2. В RabbitMQ Management UI видна глубина каждой очереди.
3. Демонстрация на защите идёт по именам очередей из документации.

## Последствия

Топология содержит больше очередей и bindings. Логика подписки при этом одна. Свести события в одну очередь позже можно сменой bindings, без изменения контракта событий.
