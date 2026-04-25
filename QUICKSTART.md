# QUICKSTART

## 1) Локальный запуск
1. Установите .NET 10 SDK.
2. Проверьте `appsettings.Development.json`.
3. Выполните:
   - `dotnet restore /home/runner/work/watchly/watchly/src/Watchly.Web/Watchly.Web.csproj`
   - `dotnet run --project /home/runner/work/watchly/watchly/src/Watchly.Web/Watchly.Web.csproj`

## 2) Миграции БД
- Linux/macOS: `bash /home/runner/work/watchly/watchly/init-database.sh`
- Windows: `pwsh /home/runner/work/watchly/watchly/init-database.ps1`

## 3) Тесты
- `dotnet test /home/runner/work/watchly/watchly/tests/Watchly.Tests/Watchly.Tests.csproj`

## 4) Docker
1. `cp .env.example .env`
2. Укажите секреты в `.env`
3. `docker compose up --build`
