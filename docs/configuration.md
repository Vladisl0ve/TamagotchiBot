# Конфигурация

Три источника настроек. Секреты в вики не копируются — только имена полей.

## 1. `config.json`

Файл лежит **рядом с exe/dll**, не в исходниках. При отсутствии `Program.CreateDefaultConfig()` пишет шаблон и процесс выходит.

Две секции:

### `TamagotchiDatabaseSettings`

Имена коллекций и БД. Поле `ConnectionString` в шаблоне — подсказка: реальное подключение **не** из этого поля, а из env (см. ниже). `DatabaseName` по умолчанию `TamagotchiDb`.

Полный список имён коллекций — в [модели данных](data-model.md) и в `CreateDefaultConfig()`.

### `EnvsSettings`

Класс: [`TamagotchiBot/Database/EnvsSettings.cs`](../TamagotchiBot/Database/EnvsSettings.cs).

| Поле | Назначение |
|------|------------|
| `TokenBot` | Есть в JSON, но клиент Telegram берёт токен из Mongo |
| `AlwaysNotifyUsers` | Список user id с доступом к `AdminController` |
| `ChatsToDevNotify` | Чаты, из которых апдейты игнорируются; туда же `DevNotifyJob` |
| `BannedRenamingUsers` | Кому нельзя переименовывать питомца |
| `DevNotifyEvery` / `DevExtraNotifyEvery` | Интервалы dev-heartbeat |
| `TriggersEvery` | Интервал триггеров (наследие таймеров) |
| `AwakeWhenAFKFor` | Порог AFK |
| `BotstatApiKey` | Botstat |
| `OpenAiApiKey` | OpenAI (дублируется живым ключом в `ServiceInfo`) |
| `ChatToForwardId` | Куда `ForwardController` шлёт сообщения |

## 2. Переменные окружения Mongo

[`MongoServiceBase.GetConnectStringFromEnv()`](../TamagotchiBot/Services/Mongo/MongoServiceBase.cs):

| Переменная | Роль |
|------------|------|
| `MongoUsername` | логин |
| `MongoPass` | пароль |
| `MongoIP` | хост |
| `MongoPort` | порт |

Scope: на Windows — `EnvironmentVariableTarget.User`, на Linux — `Process` (как в `start_bot.sh` через `export`).

Если логин/пароль пустые, строка вида `mongodb://{ip}:{port}`. Если нет IP или порта — подключение не соберётся.

Docker Compose прокидывает те же имена (`TamagotchiBot/docker-compose.yml` и корневой `docker-compose.yml`).

## 3. Mongo `ServiceInfo`

Живые секреты и операционные флаги, которые можно менять без перевыкладки конфига. Модель: [`ServiceInfo.cs`](../TamagotchiBot/Models/Mongo/ServiceInfo.cs), доступ: `SInfoService`.

| Поле | Назначение |
|------|------------|
| `TmgToken` | Токен бота для `ITelegramBotClient` |
| `GeminiApiKey` / `GeminiModel` / `MaxGeminiHistory` | Чат питомца |
| `OpenAiKey` | Legacy ChatGPT-путь |
| `SubgramKey` | Спонсорские задания |
| `DoMaintainWorks` | Включить `MaintenanceJob` на один проход |
| `DoSendChangelogs` | Включить рассылку changelog |
| `BannedWords` | Фильтр имён и т.п. |
| `BotstatCheckId` | Проверка Botstat |
| `LastGlobalUpdate` / `NextNotify` / `NextDevNotify` | Служебные таймеры |

Без заполненного `TmgToken` хост упадёт на создании клиента.

## Логи

Serilog: Verbose в файл `Logs/{yyyy}/{MM}/{dd}-.txt`, в консоль — Debug+. Категории `Microsoft` — Error, `System` и `Quartz` — Warning.

## Что не класть в git

`config.json` с реальными токенами, значения `MongoPass` / ключей в скриптах деплоя. В репозитории `linux_deploy/start_bot.sh` содержит плейсхолдеры — на сервере подставляются свои значения.
