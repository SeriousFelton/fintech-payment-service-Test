Платёжный сервис (Fintech Payment Service)

Сервис для обработки платежей с поддержкой идемпотентности, callback-квитанций и автоматического восстановления после сбоев.

Запуск

Требования

- Docker

- Docker Compose

Стек технологий

.NET 10 - основная платформа

ASP.NET Core Web API — REST API

Entity Framework Core — ORM

PostgreSQL — база данных

Polly — политики повторных попыток и Circuit Breaker

Docker \& Docker Compose — контейнеризация

 Архитектура

Domain - бизнес-сущности

Application - DTO и интерфейсы

Infrastructure - реализация внешних зависимостей (БД, HTTP-клиент)

API - контроллеры, Middleware, регистрация сервисов


 Команды

```bash

 Клонировать репозиторий

git clone https://github.com/SeriousFelton/fintech-payment-service-Test.git

cd fintech-payment-service-Test

 Запустить сервис

docker compose up --build

Сервис будет доступен по адресу: http://localhost:8080
```

## Проверка работы сервиса (сквозной сценарий)

После запуска сервиса (`docker compose up --build`) выполните следующие команды в терминале (PowerShell, CMD или Bash).

### 1. Проверка запуска сервиса
```bash
curl http://localhost:8080/health
```
или
```powershell
Invoke-RestMethod -Uri "http://localhost:8080/health"
```
**Ожидаемый ответ:** `{"status":"healthy","timestamp":"..."}`

### 2. Создание операции
```bash
curl -X POST http://localhost:8080/operations -H "Content-Type: application/json" -d '{\"operationId\":\"test-001\",\"amount\":\"100.00\",\"currency\":\"RUB\"}'
```
или
```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/operations" -ContentType "application/json" -Body '{"operationId":"test-001","amount":"100.00","currency":"RUB"}'
```
**Ожидаемый ответ:** `201 Created` с данными операции и `"status": "CREATED"`.

### 3. Отправка операции провайдеру
```bash
curl -X POST http://localhost:8080/operations/test-001/submit
```
или
```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/operations/test-001/submit"
```
**Ожидаемый ответ:** `200 OK` с `"status": "PROCESSING"` и полем `providerPaymentId`.

### 4. Проверка статуса операции
```bash
curl http://localhost:8080/operations/test-001
```
или
```powershell
Invoke-RestMethod -Uri "http://localhost:8080/operations/test-001"
```
**Ожидаемый ответ:** `"status": "COMPLETED"` (через несколько секунд, после получения callback).

### 5. Проверка истории событий
```bash
curl http://localhost:8080/operations/test-001/events
```
или
```powershell
Invoke-RestMethod -Uri "http://localhost:8080/operations/test-001/events"
```
**Ожидаемый ответ:** Массив событий: `CREATED` → `SUBMITTED` → `PROVIDER_ACCEPTED` → `RECEIPT_RECEIVED`.
