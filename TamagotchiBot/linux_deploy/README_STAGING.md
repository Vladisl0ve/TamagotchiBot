# Instructions for running the bot on Linux

## 1. File preparation
1. Copy the `start_bot_STAGING.sh` file to the same directory on the server where the bot is located (`/home/vladislove/TamagotchiBot_Staging/linux_deploy`).
2. Make the script and the bot executable:
   ```bash
   chmod +x /home/vladislove/TamagotchiBot_Staging/linux_deploy/start_bot_STAGING.sh
   ```

## 2. Running in Screen with auto-restart
The `start_bot_STAGING.sh` script already contains logic for automatic restart if the bot crashes.

1. Create a screen session:
   ```bash
   screen -S tamagotchi_staging
   ```

2. Run the script:
   ```bash
   /home/vladislove/TamagotchiBot_Staging/linux_deploy/start_bot_STAGING.sh
   ```

3. To exit screen while leaving the bot running (detach), press:
   `Ctrl+A`, then `D`.

## 3. Setting up a daily restart at 15:00
We will use `cron` to force-stop the bot at 15:00. The `start_bot_STAGING.sh` script will detect that the bot has stopped and will start it again.

1. Open the crontab editor:
   ```bash
   crontab -e
   ```

2. Add the following line to the end of the file:
   ```cron
   0 15 * * * pkill -f "/home/vladislove/TamagotchiBot_Staging/TamagotchiBot.dll"
   ```
   *Explanation: This command sends a termination signal to the running .NET process that hosts TamagotchiBot.dll exactly at 15:00. The wrapper script (start_bot_STAGING.sh) will detect that the process has exited and will start it again after 5 seconds.

## 4. Useful commands
- **Return to the bot console**: `screen -r tamagotchi_staging`
- **List screen sessions**: `screen -ls`
- **Force-kill the session**: `screen -X -S tamagotchi_staging quit`