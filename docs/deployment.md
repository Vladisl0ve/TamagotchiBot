# Деплой

Прод крутится на Linux через `screen` + watchdog-скрипт + cron, **не** systemd. MaintanceAlertBot отдельных скриптов деплоя не имеет.

Пошаговые команды (пути на сервере, screen, cron) не дублируются здесь — они уже в:

- prod: [`TamagotchiBot/linux_deploy/README.md`](../TamagotchiBot/linux_deploy/README.md) (EN + RU)
- staging: [`TamagotchiBot/linux_deploy/README_STAGING.md`](../TamagotchiBot/linux_deploy/README_STAGING.md)

## Production

1. Сборка `Release` → сборка `TamagotchiBot.Production`.
2. Рядом с бинарём: актуальный `config.json`, скрипт [`start_bot.sh`](../TamagotchiBot/linux_deploy/start_bot.sh).
3. В скрипте `export` переменных `MongoUsername`, `MongoPass`, `MongoIP`, `MongoPort`.
4. Запуск в `screen`; цикл `while true` перезапускает процесс через 5 секунд после выхода.
5. Ежедневный рестарт в 15:00: cron шлёт SIGINT процессу, watchdog поднимает его снова.

Токен бота должен быть в Mongo `ServiceInfo.TmgToken` до старта.

## Staging

Тот же паттерн: `start_bot_STAGING.sh`, сессия `tamagotchi_staging`, сборка с `STAGING` (`TamagotchiBot.Staging`). В этом режиме Subgram-реклама в `UpdateHandler` не показывается (`#if DEBUG || DEBUG_NOTIFY || STAGING`).

## Docker

Файлы:

- [`TamagotchiBot/dockerfile`](../TamagotchiBot/dockerfile) — multi-stage: restore/build/publish, entrypoint `dotnet TamagotchiBot.dll`.
- [`TamagotchiBot/docker-compose.yml`](../TamagotchiBot/docker-compose.yml) и корневой [`docker-compose.yml`](../docker-compose.yml) — сервис `tamagotchibot`, `network_mode: host`, Mongo через env.

**Расхождение:** проект таргетит `net10.0`, а Dockerfile всё ещё `mcr.microsoft.com/dotnet/sdk:8.0` и `runtime:8.0`. Образ в текущем виде не соответствует TFM приложения. Прод по факту идёт через native/linux_deploy, не через этот Dockerfile.

В образе нет `config.json`: его нужно монтировать или класть в `/app` отдельно. Mongo-креды — только через environment, не в слоях образа.

## CI

[`.github/workflows/dotnet.yml`](../.github/workflows/dotnet.yml):

- триггер: push/PR в `master`;
- `setup-dotnet` 10.0.x;
- `dotnet restore` → `build` → `test`.

Публикации, Docker-push и деплоя в Actions нет.

## Обслуживание основного бота

Два разных механизма:

1. Выключить TamagotchiBot и запустить [MaintanceAlertBot](maintenance-bot.md) на **том же токене** — игроки видят «бот на обновлении».
2. Флаг `ServiceInfo.DoMaintainWorks` при **работающем** основном боте — `MaintenanceJob` чистит orphan-пользователей, игровой бот при этом продолжает отвечать.
