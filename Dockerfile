# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy sln and csproj, restore as separate layers
COPY *.sln .
COPY DiscordBot/*.csproj ./DiscordBot/
RUN dotnet restore

# Copy all bot source files
COPY DiscordBot/. ./DiscordBot/

# Build and publish (portable, self-contained if you want)
WORKDIR /src/DiscordBot
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

# Healthy, minimal entrypoint
ENTRYPOINT ["dotnet", "DiscordBot.dll"]
