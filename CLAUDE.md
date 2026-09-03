# CLAUDE.md

Контекст проекта для Claude Code. Читай этот файл перед началом работы в репозитории.

## О проекте

Personal Finance Tracker — приложение для учёта личных доходов и расходов. Два клиента (веб и мобильный) — **один и тот же код**: Blazor WASM, собранный как PWA. Мобильное приложение работает офлайн (локальная БД в браузере) и автоматически синхронизируется с сервером при подключении к интернету.

## Технологический стек

- **Backend**: ASP.NET Core Web API, .NET 8+, C#
- **Client**: Blazor WASM (PWA), IndexedDB для локального хранения
- **БД**: PostgreSQL, EF Core (Npgsql), snake_case naming convention
- **Application слой**: MediatR (CQRS) + FluentValidation (pipeline behaviors)
- **Auth**: JWT (access + refresh token с ротацией)

## Архитектура — Clean Architecture

```
PersonalFinance.Domain          — сущности, enum'ы, без зависимостей от других слоёв
PersonalFinance.Application     — CQRS команды/запросы, DTO, интерфейсы (IAppDbContext)
PersonalFinance.Infrastructure  — EF Core, PostgreSQL, JWT, серверная часть sync
PersonalFinance.Api             — Web API, DI-регистрация, MediatR pipeline
PersonalFinance.Client          — Blazor WASM PWA
  ├─ Client.Data                — обёртка над IndexedDB (JS interop)
  ├─ Client.Sync                — Outbox, SyncService, online/offline detection
  └─ Client.Services            — локальные команды поверх IndexedDB
```

Зависимости идут строго внутрь: `Api → Infrastructure → Application → Domain`. Web ссылается на Infrastructure только для DI в `Program.cs`. Blazor-компоненты общаются с бэкендом только через `IMediator`, никогда напрямую с DbContext.

## Ключевые архитектурные принципы (обязательны к соблюдению)

1. **Без Repository/UnitOfWork.** Доступ к данным — напрямую через `IAppDbContext` (тонкий интерфейс над `DbSet<T>`). Интерфейс существует только ради мокирования в юнит-тестах хендлеров, не добавляй поверх него абстракций.
2. **CQRS через MediatR.** Каждая операция изменения/чтения — отдельная `Command`/`Query` + `Handler`. Валидация — через `FluentValidation` + `ValidationBehavior` в pipeline, не внутри хендлеров.
3. **Guid Id генерируются на клиенте** (`Guid.NewGuid()` в конструкторе/при создании), не на сервере и не автоинкрементом в БД. Это критично для офлайн-создания записей без коллизий при синхронизации.
4. **Soft delete везде.** Никогда не удаляй записи физически — `IsDeleted = true` + обновление `UpdatedAt`. Удаления должны реплицироваться через sync так же, как обычные изменения.
5. **`UpdatedAt` — обязательное поле каждой сущности** (уже в `BaseEntity`). Используется и для Last-Write-Wins при конфликтах синхронизации, и как курсор в delta-pull запросах (`WHERE updated_at > @since`).
6. **PostgreSQL: snake_case.** Именование колонок/таблиц — через `UseSnakeCaseNamingConvention()`, не переопределяй вручную через `HasColumnName`. Схема БД — `finance`.
7. **Sync — outbox pattern, не двусторонний realtime.** Клиент копит изменения в локальной очереди Outbox, при появлении сети — push батчем на сервер, затем pull дельты с курсором `lastSyncedAt`. Конфликты — Last-Write-Wins по `UpdatedAt`. Не усложняй до CRDT/vector clocks — для одного пользователя на 1-3 устройствах это избыточно.

## Текущий статус

**Готово:**
- Domain: `BaseEntity`, `User`, `Account`, `Category`, `Transaction`, enums (`CategoryType`, `TransactionType`)
- Infrastructure: `AppDbContext` (схема `finance`, snake_case, auto-update `UpdatedAt`), Fluent API конфигурации всех сущностей, soft-delete query filters, `AppDbContextFactory` для design-time миграций

**Следующие шаги (по порядку):**
1. Application слой — команды/запросы MediatR для Account/Category/Transaction (Create/Update/Delete/GetList) + FluentValidation
2. Api — контроллеры/minimal API, JWT-аутентификация, регистрация MediatR pipeline
3. Client — настройка Blazor WASM PWA, обёртка над IndexedDB
4. Sync — Outbox на клиенте, `POST /api/sync/push` и `GET /api/sync/pull` на сервере

## Команды разработки

```bash
# Миграции (выполнять из папки Infrastructure)
dotnet ef migrations add <Name> --startup-project ../PersonalFinance.Api
dotnet ef database update --startup-project ../PersonalFinance.Api

# Сборка и тесты
dotnet build
dotnet test

# Запуск API
dotnet run --project PersonalFinance.Api
```

Строка подключения ожидается в конфигурации как `ConnectionStrings:PostgreSql`.

## Соглашения по коду

- File-scoped namespaces (`namespace X;`, без фигурных скобок на весь файл)
- `Nullable` enabled, явно помечать nullable-поля (`string? Note`)
- Именование команд/запросов: `{Глагол}{Сущность}Command` / `{Глагол}{Сущность}Query` (напр. `CreateTransactionCommand`, `GetTransactionsByAccountQuery`)
- Валидаторы: `{CommandName}Validator`
- Идентификаторы в коде — на английском, комментарии допустимы на русском, если поясняют неочевидную бизнес-логику (особенно логику синхронизации)
- Денежные суммы — `decimal`, precision `(18, 2)` на уровне EF конфигурации
- Даты транзакций — `DateOnly` (не `DateTime`), точное время не нужно для учёта расходов

## Чего не делать

- Не добавлять Repository/UnitOfWord поверх `IAppDbContext`
- Не использовать server-generated ID (identity/serial) для сущностей, которые могут создаваться офлайн
- Не делать hard delete
- Не проектировать sync как realtime/websocket-синхронизацию — только явный push/pull по требованию клиента
- Не переусложнять разрешение конфликтов (без CRDT, без ручного merge UI на первой итерации)
