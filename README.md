# Watchly

Watchly — ASP.NET Core 10 MVC приложение для каталога фильмов, избранного и истории просмотров.

## Возможности
- Регистрация/логин через ASP.NET Identity
- Каталог фильмов с фильтрацией и сортировкой
- Детальная страница фильма
- Избранное (Watchlist)
- История просмотров
- Админ-панель для CRUD фильмов
- Service + Repository архитектура
- FluentValidation
- EF Core миграции

## Технологии
- ASP.NET Core MVC 10
- Entity Framework Core + SQL Server
- ASP.NET Core Identity
- FluentValidation
- xUnit + Moq

## Запуск локально
```bash
dotnet restore /home/runner/work/watchly/watchly/src/Watchly.Web/Watchly.Web.csproj
dotnet run --project /home/runner/work/watchly/watchly/src/Watchly.Web/Watchly.Web.csproj
```

## Тесты
```bash
dotnet test /home/runner/work/watchly/watchly/tests/Watchly.Tests/Watchly.Tests.csproj
```

## Docker
```bash
cp .env.example .env
docker compose up --build
```

Подробнее: `QUICKSTART.md`.
