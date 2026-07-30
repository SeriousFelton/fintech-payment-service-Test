\Платёжный сервис (Fintech Payment Service)

Сервис для обработки платежей с поддержкой идемпотентности, callback-квитанций и автоматического восстановления после сбоев.

\Запуск

\ Требования

\- Docker

\- Docker Compose



\ Команды

```bash

\ Клонировать репозиторий

git clone https://github.com/SeriousFelton/fintech-payment-service-Test.git

cd fintech-payment-service-Test

\ Запустить сервис

docker compose up --build

Сервис будет доступен по адресу: http://localhost:8080

\ Стек технологий

.NET 10 - основная платформа

ASP.NET Core Web API — REST API

Entity Framework Core — ORM

PostgreSQL — база данных

Polly — политики повторных попыток и Circuit Breaker

Docker \& Docker Compose — контейнеризация

\ Архитектура

Domain - бизнес-сущности

Application - DTO и интерфейсы

Infrastructure - реализация внешних зависимостей (БД, HTTP-клиент)

API - контроллеры, Middleware, регистрация сервисов

