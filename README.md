# Personal Finance Tracker

Приложение для учёта личных доходов и расходов с веб- и мобильной версией. Мобильная версия работает офлайн и автоматически синхронизируется при подключении к интернету.

## Возможности

- Учёт доходов и расходов по категориям
- Несколько счетов/кошельков, переводы между ними
- Работа полностью офлайн на мобильном устройстве
- Автоматическая синхронизация между устройствами при появлении сети
- Один код для веб и мобильной версии (PWA)

## Технологический стек

| Слой | Технологии |
|---|---|
| Backend | ASP.NET Core Web API, .NET 8, MediatR (CQRS), FluentValidation |
| БД | PostgreSQL, EF Core (Npgsql), snake_case naming |
| Клиент | Blazor WASM (PWA), IndexedDB |
| Аутентификация | JWT (access + refresh token) |

## Архитектура

Clean Architecture, зависимости идут строго внутрь: `Api → Infrastructure → Application → Domain`.

```
PersonalFinance.Domain          — сущности, enum'ы
PersonalFinance.Application     — CQRS команды/запросы, DTO
PersonalFinance.Infrastructure  — EF Core, PostgreSQL, JWT, sync
PersonalFinance.Api             — Web API
PersonalFinance.Client          — Blazor WASM PWA
  ├─ Client.Data                — IndexedDB
  ├─ Client.Sync                — Outbox, SyncService
  └─ Client.Services            — локальные команды поверх IndexedDB
```

Данные не удаляются физически (soft delete) и не используют серверные автоинкрементные ID — все сущности получают `Guid` на клиенте в момент создания, что позволяет создавать записи офлайн без конфликтов идентификаторов. Синхронизация построена по паттерну outbox с разрешением конфликтов Last-Write-Wins по полю `UpdatedAt`.

Подробнее об архитектурных принципах и соглашениях — в [`CLAUDE.md`](./CLAUDE.md).

## Требования

- .NET 8 SDK
- PostgreSQL 15+
- `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## Запуск

1. Клонировать репозиторий и восстановить зависимости:

   ```bash
   dotnet restore
   ```

2. Указать строку подключения в `PersonalFinance.Api/appsettings.Development.json`:

   ```json
   {
     "ConnectionStrings": {
       "PostgreSql": "Host=localhost;Port=5432;Database=personal_finance;Username=postgres;Password=postgres"
     }
   }
   ```

3. Применить миграции:

   ```bash
   cd PersonalFinance.Infrastructure
   dotnet ef database update --startup-project ../PersonalFinance.Api
   ```

4. Запустить API:

   ```bash
   dotnet run --project PersonalFinance.Api
   ```

5. Запустить клиент:

   ```bash
   dotnet run --project PersonalFinance.Client
   ```

## Статус разработки

- [x] Domain-модель (Account, Category, Transaction, User)
- [x] EF Core + PostgreSQL схема
- [ ] Application-слой (команды/запросы MediatR)
- [ ] Web API + JWT-аутентификация
- [ ] Клиент: Blazor WASM PWA + IndexedDB
- [ ] Механизм офлайн-синхронизации (Outbox, push/pull)

## Разработка с Claude Code

Проект использует [`CLAUDE.md`](./CLAUDE.md) для контекста — там описаны архитектурные принципы, соглашения по коду и текущий прогресс.
