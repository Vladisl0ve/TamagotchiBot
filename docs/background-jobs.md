# Фоновые задачи

Планировщик — **Quartz** 3.7.0 (`AddQuartz` + `AddQuartzHostedService` в [`Program.cs`](../TamagotchiBot/Program.cs)).

Рабочие реализации: пространство имён `TamagotchiBot.Jobs`, папка [`TamagotchiBot/Jobs/`](../TamagotchiBot/Jobs/).

Decay статов питомца **не** джоба: он в `CreatorController.UpdateIndicators` при апдейте пользователя ([игровая логика](game-mechanics.md)).

## Активные джобы

| Джоба | Расписание | Назначение |
|-------|------------|------------|
| `ResetAllExpJob` | cron `0 0 1 1 * ?` — 1 января 01:00 | Топ-10 по уровню, награды, сброс сезонного EXP |
| `AutoFeedJob` | `Constants.CronSchedule.AutoFeedCron` = `0 0 0/4 * * ?` (каждые 4 часа) | Кормит питомцев с `IsAutoFeedEnabled`, списывает `AutoFeedCharges` |
| `DailyRewardJob` | каждые 5 мин (при `DEBUG_NOTIFY` — каждые 30 с) | Напоминание, что можно забрать ежедневную награду |
| `RandomEventJob` | каждые 10 мин (при `DEBUG_NOTIFY` — каждые 10 с) | `RandomEventService.DoRandomEvent`; ночью 20:00–04:00 UTC не шлёт |
| `MaintenanceJob` | каждую 1 мин | Если `ServiceInfo.DoMaintainWorks` — чистка orphan-записей, затем флаг сбрасывается |
| `ChangelogJob` | каждые 30 с | Если `DoSendChangelogs` — рассылка changelog и сброс флага |
| `DuelTimeoutJob` | каждые 100 с | Истечение дуэлей старше `TimesToWait.DuelCDToWait` (5 мин) |
| `DevNotifyJob` | каждые 60 с | Heartbeat в `ChatsToDevNotify`; тело выполняется только при `#if RELEASE` |

## Наследие (не runtime)

- [`TamagotchiBot/Services/Jobs/`](../TamagotchiBot/Services/Jobs/) — дубликаты тех же классов в другом namespace. Quartz регистрирует **только** `TamagotchiBot.Jobs`.
- [`NotifyTimerService`](../TamagotchiBot/Services/NotifyTimerService.cs) — старый таймерный цикл; в DI **не** регистрируется.

Не путать `MaintenanceJob` с процессом [`MaintanceAlertBot`](maintenance-bot.md): джоба чистит БД по флагу, alert-бот отвечает игрокам, пока основной процесс выключен.

## Операционные флаги

Включение разовых массовых действий — поля `ServiceInfo`, не конфиг:

- `DoMaintainWorks` → `MaintenanceJob`
- `DoSendChangelogs` → `ChangelogJob`

Джоба сама вызывает `DisableMaintainWorks()` / `DisableChangelogsSending()` после старта, чтобы не крутить рассылку бесконечно.
