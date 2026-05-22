#!/bin/bash

# Запускаем API в фоне
dotnet /api/LocalScope.Api.dll --urls http://0.0.0.0:5108 &

# Запускаем Nginx на переднем плане
nginx -g "daemon off;"