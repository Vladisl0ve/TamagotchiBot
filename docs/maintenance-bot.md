# MaintanceAlertBot

Отдельный exe в [`MaintanceAlertBot/`](../MaintanceAlertBot/): тонкая заглушка, пока основной TamagotchiBot выключен (релиз, миграция). Игровой логики, Quartz и мини-игр нет.

Опечатка в имени проекта (`Maintance`) сохранена как есть.

## Зачем

Один и тот же Telegram-токен не может крутить два long-polling процесса. На время даунтайма основного бота поднимают MaintanceAlertBot: любой текст получает локализованное предупреждение и ссылку на канал новостей.

## Поведение

[`MaintanceAlertBot/Handlers/UpdateHandler.cs`](../MaintanceAlertBot/Handlers/UpdateHandler.cs):

- обрабатывает `Message` и `EditedMessage`, только текст;
- культура: `UserService.Get` из общей БД, иначе `"ru"`;
- ответ: стикер changelog + `BotOnMaintanceWarning` + `changelogCommand` со ссылкой `https://t.me/news_virtualpetbot`;
- reply-клавиатура меню и inline-кнопка в канал;
- при `ApiRequestException` — лог и `Environment.Exit(-1)`.

## Связь с TamagotchiBot

| Что общее | Как |
|-----------|-----|
| ProjectReference | `MaintanceAlertBot.csproj` → TamagotchiBot |
| Токен | `SInfoService.GetBotToken()` → `ServiceInfo.TmgToken` |
| Культура игрока | `TamagotchiBot.Services.Mongo.UserService` |
| Тексты, стикеры, клавиатуры | Resources и `Constants` основного бота |

Локальные копии `TamagotchiDatabaseSettings` / `ServiceInfo` внутри MaintanceAlertBot для шаблона `config.json` есть; runtime DI использует типы TamagotchiBot.

## Запуск

Тот же контракт, что у основного бота:

1. `config.json` рядом с exe (минимум секция `TamagotchiDatabaseSettings` с `DatabaseName` и `ServiceInfoCollectionName`).
2. Env `MongoUsername` / `MongoPass` / `MongoIP` / `MongoPort`.
3. В БД заполнен `TmgToken`.
4. Основной TamagotchiBot **остановлен**, иначе Telegram отдаст конфликт getUpdates.

Отдельных Dockerfile / `linux_deploy` для alert-бота нет: консольный `dotnet run` / опубликованный exe.

## Не путать с MaintenanceJob

| | MaintanceAlertBot | `MaintenanceJob` |
|--|-------------------|------------------|
| Процесс | отдельный exe | Quartz внутри TamagotchiBot |
| Триггер | человек выключил основной бот | флаг `DoMaintainWorks` |
| Действие | отвечает «идёт обновление» | удаляет orphan user/pet данные |

Подробнее про джобу — [фоновые задачи](background-jobs.md).
