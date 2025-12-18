# Instructions for running the bot on Linux

## 1. File preparation
1. Copy the `start_bot.sh` file to the same directory on the server where the bot is located (`/home/vladislove/Tamagotchi`).
2. Make the script and the bot executable:
   ```bash
   chmod +x /home/vladislove/Tamagotchi/start_bot.sh
   chmod +x /home/vladislove/Tamagotchi/TamagotchiBot
   ```

## 2. Running in Screen with auto-restart
The `start_bot.sh` script already contains logic for automatic restart if the bot crashes.

1. Create a screen session:
   ```bash
   screen -S tamagotchi
   ```

2. Run the script:
   ```bash
   /home/vladislove/Tamagotchi/start_bot.sh
   ```

3. To exit screen while leaving the bot running (detach), press:
   `Ctrl+A`, then `D`.

## 3. Setting up a daily restart at 15:00
We will use `cron` to force-stop the bot at 15:00. The `start_bot.sh` script will detect that the bot has stopped and will start it again.

1. Open the crontab editor:
   ```bash
   crontab -e
   ```

2. Add the following line to the end of the file:
   ```cron
   0 15 * * * killall -s SIGINT TamagotchiBot
   ```
   *If the `killall` command is not found, try `pkill -f TamagotchiBot`.*

   *Explanation:* This command sends a termination signal to the bot exactly at 15:00. The wrapper script (`start_bot.sh`) will detect that the process has exited and will start it again after 5 seconds.

## 4. Useful commands
- **Return to the bot console**: `screen -r tamagotchi`
- **List screen sessions**: `screen -ls`
- **Force-kill the session**: `screen -X -S tamagotchi quit`





# Инструкция по запуску бота на Linux

## 1. Подготовка файлов
1. Скопируйте файл `start_bot.sh` в ту же папку на сервере, где лежит бот (`/home/vladislove/Tamagotchi`).
2. Сделайте скрипт и бота исполняемыми:
   ```bash
   chmod +x /home/vladislove/Tamagotchi/start_bot.sh
   chmod +x /home/vladislove/Tamagotchi/TamagotchiBot
   ```

## 2. Запуск в Screen c авто-перезапуском
Скрипт `start_bot.sh` уже содержит логику автоматического перезапуска при падении.

1. Создайте сессию screen:
   ```bash
   screen -S tamagotchi
   ```

2. Запустите скрипт:
   ```bash
   /home/vladislove/Tamagotchi/start_bot.sh
   ```

3. Чтобы выйти из screen, оставив бота работать (detach), нажмите:
   `Ctrl+A`, затем `D`.

## 3. Настройка ежедневного перезапуска в 15:00
Мы будем использовать `cron`, чтобы принудительно остановить бота в 15:00. Скрипт `start_bot.sh` увидит, что бот остановился, и запустит его заново.

1. Откройте редактор crontab:
   ```bash
   crontab -e
   ```

2. Добавьте следующую строку в конец файла:
   ```cron
   0 15 * * * killall -s SIGINT TamagotchiBot
   ```
   *Если команда `killall` не найдена, попробуйте `pkill -f TamagotchiBot`.*

   *Пояснение:* Команда посылает сигнал завершения боту ровно в 15:00. Скрипт-обертка (`start_bot.sh`) обнаружит завершение процесса и снова запустит его через 5 секунд.

## 4. Полезные команды
- **Вернуться в консоль бота**: `screen -r tamagotchi`
- **Посмотреть список screen сессий**: `screen -ls`
- **Принудительно убить сессию**: `screen -X -S tamagotchi quit`
