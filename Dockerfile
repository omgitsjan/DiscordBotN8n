# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY DiscordBot/*.csproj ./DiscordBot/
COPY DiscordBotTests/*.csproj ./DiscordBotTests/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY DiscordBot/. ./DiscordBot/
COPY DiscordBotTests/. ./DiscordBotTests/

# Run tests (fail build if any test fails)
WORKDIR /src/DiscordBotTests
RUN dotnet test --no-restore --verbosity normal

# Build and publish the main app (only if tests passed)
WORKDIR /src/DiscordBot
RUN dotnet publish -c Release -o /app

# Stage 2: Setup runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "DiscordBot.dll"]
