# Сборка API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-api
WORKDIR /src
COPY . .
RUN dotnet restore LocalScope.Api/LocalScope.Api.csproj
RUN dotnet publish LocalScope.Api/LocalScope.Api.csproj -c Release -o /app/api

# Сборка Web (Blazor)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-web
WORKDIR /src
COPY . .
RUN dotnet restore LocalScope.Web/LocalScope.Web.csproj
RUN dotnet publish LocalScope.Web/LocalScope.Web.csproj -c Release -o /app/web

# Финальный образ
FROM nginx:alpine

# Установка .NET runtime для API
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Копируем API
COPY --from=build-api /app/api /api

# Копируем Blazor Web в nginx
COPY --from=build-web /app/web/wwwroot /usr/share/nginx/html

# Копируем конфиг nginx
COPY nginx.conf /etc/nginx/nginx.conf

# Устанавливаем .NET runtime
RUN apk add --no-cache bash wget && \
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir /usr/share/dotnet && \
    ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

EXPOSE 80 5108

# Скрипт запуска API и Nginx
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]