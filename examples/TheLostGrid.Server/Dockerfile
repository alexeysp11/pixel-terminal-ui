# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Stage 1: Basic server runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# Comment out USER $APP_UID so that docker exec loads terminal environment variables (TERM, LANG) correctly without any permission conflicts
# USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Stage 3: Copy only the .csproj files to cache the dotnet restore layers
COPY ["examples/TheLostGrid.Server/TheLostGrid.Server.csproj", "examples/TheLostGrid.Server/"]
COPY ["examples/TheLostGrid.Client/TheLostGrid.Client.csproj", "examples/TheLostGrid.Client/"]
COPY ["src/PixelTerminalUI.Contracts/PixelTerminalUI.Contracts.csproj", "src/PixelTerminalUI.Contracts/"]
COPY ["src/PixelTerminalUI.Persistence.Redis/PixelTerminalUI.Persistence.Redis.csproj", "src/PixelTerminalUI.Persistence.Redis/"]
COPY ["src/PixelTerminalUI.StatelessEngine/PixelTerminalUI.StatelessEngine.csproj", "src/PixelTerminalUI.StatelessEngine/"]

# Restore dependencies for both end applications
RUN dotnet restore "./examples/TheLostGrid.Server/TheLostGrid.Server.csproj"
RUN dotnet restore "./examples/TheLostGrid.Client/TheLostGrid.Client.csproj"
COPY . .

# Assembling the server part
WORKDIR "/src/examples/TheLostGrid.Server"
RUN dotnet build "./TheLostGrid.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build/server

# Assembling the client part
WORKDIR "/src/examples/TheLostGrid.Client"
RUN dotnet build "./TheLostGrid.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build/client

# Stage 3: Publishing Artifacts
FROM build AS publish
ARG BUILD_CONFIGURATION=Release

WORKDIR "/src/examples/TheLostGrid.Server"
RUN dotnet publish "./TheLostGrid.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish/server /p:UseAppHost=false

WORKDIR "/src/examples/TheLostGrid.Client"
RUN dotnet publish "./TheLostGrid.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish/client /p:UseAppHost=false

# Stage 4: Final Production Look
FROM base AS final
WORKDIR /app

# Copy the server application directly to the root /app
COPY --from=publish /app/publish/server .

# Copy the client application to the isolated subfolder /app/client
COPY --from=publish /app/publish/client ./client

# Linux locale settings inside the container so that TUI pseudo-graphics and Cyrillic characters don't turn into question marks
ENV LANG=en_US.UTF-8
ENV LC_ALL=en_US.UTF-8

ENTRYPOINT ["dotnet", "TheLostGrid.Server.dll"]