# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY DiscordBot/*.csproj ./DiscordBot/
RUN dotnet restore

# Copy everything else and build
COPY DiscordBot/. ./DiscordBot/

# Build the project
WORKDIR /src/DiscordBot
RUN dotnet publish -c Release -o /app

# Stage 2: Setup runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
