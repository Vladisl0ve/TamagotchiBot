# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TamagotchiBot.csproj", "./"]
RUN dotnet restore "TamagotchiBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TamagotchiBot.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "TamagotchiBot.csproj" -c Release -o /app/publish

# Stage 3: Final
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Environment variables should be passed via docker-compose or run command
# Defaults can be set here if necessary, but it's better to keep secrets out of the image

ENTRYPOINT ["dotnet", "TamagotchiBot.dll"]
