# Use the official .NET runtime image as the base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app

# Copy all files from the current directory on the host to /app in the container
COPY . /app/TamagotchiBot

# Expose any ports the application is listening on (optional)
EXPOSE 80
EXPOSE 443

#Add envs
ENV MongoUsername="vladislove"
ENV MongoPass="123"
ENV MongoIP="127.0.0.1"
ENV MongoPort="27017"

# Command to run the application
ENTRYPOINT ["dotnet", "/app/TamagotchiBot/TamagotchiBot.dll"]
