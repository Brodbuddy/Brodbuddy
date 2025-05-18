FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG GITHUB_USERNAME
WORKDIR /src
COPY ["nuget.config", "./"]
COPY ["server/Startup/Startup.csproj", "server/Startup/"]
COPY ["server/Api.Http/Api.Http.csproj", "server/Api.Http/"]
COPY ["server/Application/Application.csproj", "server/Application/"]
COPY ["server/Core/Core.csproj", "server/Core/"]
COPY ["server/Api.Mqtt/Api.Mqtt.csproj", "server/Api.Mqtt/"]
COPY ["server/Api.Websocket/Api.Websocket.csproj", "server/Api.Websocket/"]
COPY ["server/Infrastructure.Data/Infrastructure.Data.csproj", "server/Infrastructure.Data/"]
COPY ["server/Infrastructure.Communication/Infrastructure.Communication.csproj", "server/Infrastructure.Communication/"]
COPY ["server/Infrastructure.Auth/Infrastructure.Auth.csproj", "server/Infrastructure.Auth/"]
COPY ["server/Infrastructure.Monitoring/Infrastructure.Monitoring.csproj", "server/Infrastructure.Monitoring/"]

RUN --mount=type=secret,id=github_token \
    export GITHUB_TOKEN=$(cat /run/secrets/github_token) && \
    export GITHUB_USERNAME=${GITHUB_USERNAME} && \
    dotnet restore "server/Startup/Startup.csproj"
    
COPY . .
WORKDIR "/src/server/Startup"
RUN dotnet build "Startup.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Startup.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Startup.dll"]