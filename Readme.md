
# MonitorHandler

**MonitorHandler** — это система мониторинга и управления серверами и сервисами, включающая веб-API и Telegram-бота для взаимодействия с пользователями. Проект состоит из трех частей:

- **MonitorHandler** — серверная часть (Web API на ASP.NET Core)
- **ViewTelegramBot** — Telegram-бот для управления и мониторинга
- **Database** — библиотека для работы с базой данных

---

## 1. MonitorHandler (Web API)

Серверная часть предоставляет REST API для управления серверами, контейнерами Docker, метриками и скриптами. Также предоставляет API для получение данных о состоянии серверов и контейнеров.

### Зависимости

- .NET 7.0 или выше
- ASP.NET Core
- Entity Framework Core
- Docker.DotNet (для работы с Docker)
- Newtonsoft.Json

### Запуск

```sh
dotnet run --project MonitorHandler/MonitorHandler.csproj
```

---

## 2. ViewTelegramBot

Telegram-бот для управления серверами, просмотра их метрик, управлениями контейнерами и получения уведомлений о состоянии.

### Зависимости

- .NET 7.0 или выше
- Telegram.Bot
- Newtonsoft.Json

### Запуск

```sh
dotnet run --project ViewTelegramBot/ViewTelegramBot.csproj
```

---

## 3. Database

Библиотека для работы с базой данных, используемая обеими частями проекта.

### Зависимости

- .NET Standard 2.0 или выше
- Entity Framework Core

### Сборка

```sh
dotnet build Database/Database.csproj
```

---

## Общие требования

- Установленный .NET SDK 7.0+
- Docker (для работы с контейнерами)
- Доступ к Telegram Bot API (токен бота)
