#!/bin/bash

# Directory where the bot binaries are located
WORK_DIR="/home/vladislove/Tamagotchi"
BINARY_NAME="./TamagotchiBot"

# Go to the directory
cd "$WORK_DIR" || { echo "Error: Directory $WORK_DIR not found."; exit 1; }

echo "Starting Watchdog for $BINARY_NAME..."

while true; do
    echo "---------------------------------"
    echo "Starting Bot at $(date)"
    echo "---------------------------------"
    
    # Run the bot
    $BINARY_NAME
    
    # If the bot crashes or is stopped, we wait a bit before restarting
    echo "Bot stopped. Restarting in 5 seconds..."
    sleep 5
done
