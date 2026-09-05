# Вики TamagotchiBot

Telegram-бот виртуального питомца на C# (.NET 10). Живой бот: [t.me/VirtualPetBot](https://t.me/VirtualPetBot). Канал обновлений: [t.me/news_virtualpetbot](https://t.me/news_virtualpetbot).

Это внутренняя документация репозитория: архитектура, игровая логика, данные, деплой. Маркетинговое описание для игроков остаётся в корневом [README.md](../README.md).

## Карта страниц

| Страница | О чём |
|----------|--------|
| [Архитектура](architecture.md) | Host, DI, polling, контроллеры, поток обработки |
| [Игровая логика](game-mechanics.md) | Статы питомца, decay, экономика, фичи |
| [Модель данных](data-model.md) | MongoDB: коллекции, сущности, сервисы |
| [Telegram-поток](telegram-flow.md) | Апдейты, команды, группы, платежи |
| [Фоновые задачи](background-jobs.md) | Quartz-джобы |
| [Конфигурация](configuration.md) | `config.json`, переменные окружения, `ServiceInfo` |
| [Деплой](deployment.md) | Linux, staging, Docker, CI |
| [MaintanceAlertBot](maintenance-bot.md) | Stand-in на время обслуживания |
| [Разработка](development.md) | Локальный запуск, сборка, локализация, тесты |

## Состав решения

Solution: [`TamagotchiBot.sln`](../TamagotchiBot.sln). Все проекты — `net10.0`.

| Проект | Роль |
|--------|------|
| [`TamagotchiBot`](../TamagotchiBot/) | Основной игровой бот (Exe) |
| [`MaintanceAlertBot`](../MaintanceAlertBot/) | Лёгкий бот «на обслуживании», ссылается на TamagotchiBot |
| [`TamagotchiBot.Tests`](../TamagotchiBot.Tests/) | Заготовка xUnit (пока без покрытия основного бота) |
| [`MaintanceAlertBot.Tests`](../MaintanceAlertBot.Tests/) | Заготовка тестов alert-бота |

## С чего начать

1. [Разработка](development.md) — поднять локально.
2. [Архитектура](architecture.md) — понять, как апдейт доходит до меню.
3. [Конфигурация](configuration.md) — `config.json`, Mongo env, токен в БД.
4. [Деплой](deployment.md) — prod/staging на Linux.
