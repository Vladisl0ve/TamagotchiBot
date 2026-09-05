# Разработка

## Требования

- .NET SDK **10.0.x**
- MongoDB (локально или удалённо)
- Токен тестового бота, записанный в коллекцию `ServiceInfo` (`TmgToken`)

## Локальный запуск

Из корня репозитория:

```bash
dotnet run --project TamagotchiBot/TamagotchiBot.csproj
```

Или открыть [`TamagotchiBot.sln`](../TamagotchiBot.sln) в Visual Studio / Cursor.

Первый запуск без `config.json` рядом с выходным каталогом создаст шаблон и **завершится**. Нужно заполнить `EnvsSettings` (списки админов и т.д.) и убедиться, что Mongo доступна.

Переменные Mongo (Windows — User env, Linux — process):

- `MongoUsername`, `MongoPass`, `MongoIP`, `MongoPort`

Логи: `Logs/yyyy/MM/dd-.txt` в каталоге сборки + консоль.

## Конфигурации сборки

Заданы в [`TamagotchiBot.csproj`](../TamagotchiBot/TamagotchiBot.csproj):

| Configuration | Результат |
|---------------|-----------|
| `Debug` | Обычная отладка |
| `Release` | `TamagotchiBot.Production`, символ `RELEASE` (`DevNotifyJob` шлёт heartbeat) |
| `Staging` / `Staging_Local` | `TamagotchiBot.Staging`, символ `STAGING` (без Subgram) |
| `Debug-Notify` | в csproj добавляется `DEBUG`; в коде также есть `#if DEBUG_NOTIFY` для частых DailyReward/RandomEvent |
| `Debug-Hotfix` | отдельная конфигурация без доп. символов в csproj |

MaintanceAlertBot в solution для Staging/Debug-Hotfix собирается как Debug/Release (см. `.sln`).

## Структура TamagotchiBot

```
TamagotchiBot/
  Program.cs
  Handlers/          UpdateHandler
  Controllers/       сценарии игры (не MVC)
  Services/          HostedService, BotControl, Mongo, Payment
  Jobs/              активные IJob
  Models/Mongo/      документы
  Database/          settings-классы
  UserExtensions/    Constants, клавиатуры, культура
  Resources/         ResX, имена питомцев, Hangman, промпт Gemini
  linux_deploy/      скрипты и README сервера
```

Карта слоёв — [архитектура](architecture.md).

## Локализация

- ResX: `Resources/Resources.resx` (default/en) + `.ru`, `.uk`, `.be`, `.pl`
- `Constants.Languages`: be, uk, ru, en, pl
- Культура игрока: `User.Culture`, хелперы `UseCulture()` / `LanguageExtensions` (славянские формы времени)
- Пулы имён: `Resources/{en,ru,uk,be,pl}/PetNames_*.txt`
- Виселица: `Resources/Hangman_dicts/russian.txt`
- Роль питомца для Gemini: `Resources/Prompts/GeminiPetRoleplay.txt`

`AddLocalization(ResourcesPath = "Resources")` в `Program.cs`.

## Тесты

| Проект | Состояние |
|--------|-----------|
| `TamagotchiBot.Tests` | xUnit, `UnitTest1.Test1()` пустой; **нет** `ProjectReference` на TamagotchiBot |
| `MaintanceAlertBot.Tests` | есть `TestableUpdateHandler`, **нет** `[Fact]`/`[Theory]` |

CI всё равно гоняет `dotnet test` — сейчас это фактически пустой прогон.

## Полезные ссылки

- Игровые числа: `UserExtensions/Constants.cs`
- Роутинг апдейтов: `Handlers/UpdateHandler.cs`
- Меню: `Controllers/MenuController.cs`
- Decay: `Controllers/CreatorController.UpdateIndicators`
- Настройки: [configuration.md](configuration.md)
- Выкладка: [deployment.md](deployment.md)
