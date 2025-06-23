#!/usr/bin/env sh
set -e

# Chờ database sẵn sàng (nếu cần, có thể thêm wait-for-it hoặc healthcheck)
# Ví dụ đơn giản: sleep vài giây
echo "Waiting for SQL Server to be ready…"
sleep 15

echo "Applying EF Core migrations…"
dotnet ef database update --no-build --project UserService.Infrastructure

echo "Starting API…"
exec dotnet UserService.API.dll
