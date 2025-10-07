# Портал управл### Frontend (Blazor)
- Веб-интерфейс для работы с запросами доступа
- Аутентификация через доменную учетную запись
- Управление ресурсами и группами
- Отслеживание статуса запросов

## Конфигурация

### Контроллеры домена
- Основной: `o6dc.gz.local`
- Резервные: 
  - `06dc01.gz.local`
  - `06dc02.gz.local`

### База данных
- Сервер: `06-sql01`
- База данных: `Portal`
- Пользователь: `06portaluser`доступом к ресурсам Active Directory

Приложение для управления запросами доступа к ресурсам в домене gz.local с поддержкой множественных контроллеров домена.

## Архитектура

Приложение состоит из двух основных компонентов:

### Backend (ASP.NET Core)
- REST API для работы с Active Directory
- Интеграция с несколькими контроллерами домена
- Отказоустойчивая архитектура с автоматическим переключением
- Расширенное логирование всех операций
- Кэширование и оптимизация для работы в интранете

We've given you both a frontend and backend to play around with and where you go from here is up to you!

Everything you do here is contained within this one codespace. There is no repository on GitHub yet. If and when you’re ready you can click "Publish Branch" and we’ll create your repository and push up your project. If you were just exploring then and have no further need for this code then you can simply delete your codespace and it's gone forever.

### Run Options

## Возможности

- Аутентификация через Active Directory
- Управление запросами на доступ
- Просмотр доступных ресурсов
- Управление группами доступа
- Автоматическое переключение между контроллерами домена
- Детальное логирование операций

## Требования

- .NET 9.0
- SQL Server
- Доступ к контроллерам домена gz.local
- Visual Studio 2025 (для разработки)

## Установка и запуск

1. Клонировать репозиторий:
```bash
git clone <repository-url>
```

2. Настроить подключение к базе данных в `appsettings.json`

3. Применить миграции:
```bash
cd SampleApp/BackEnd
dotnet ef database update
```

4. Запустить приложение:
   - Через Visual Studio: Открыть `PortalApp.sln` и запустить оба проекта
   - Через командную строку:
     ```bash
     # Терминал 1
     cd SampleApp/BackEnd
     dotnet run

     # Терминал 2
     cd SampleApp/FrontEnd
     dotnet run
     ```

## Логирование

Приложение использует расширенное логирование:
- Консольные логи для отладки
- Файловые логи в формате JSON (`/Logs/diagnostic-.json`)
- Отдельные логи ошибок (`/Logs/errors-.txt`)
- Контекстная информация о запросах
- Информация о переключениях между контроллерами домена

## Безопасность

- HTTPS для всех соединений
- JWT аутентификация
- Настроенные CORS политики
- Защита от XSS и CSRF атак
- Безопасные заголовки HTTP

## Мониторинг

- Отслеживание состояния контроллеров домена
- Мониторинг производительности запросов
- Статистика использования ресурсов
- Логирование действий пользователей


## Разработка

Проект можно открыть в:
- Visual Studio 2025
- Visual Studio Code с C# расширением
- JetBrains Rider

Для разработки рекомендуется использовать Feature Branches и Pull Requests.

## Контрибьюция

1. Создать ветку для новой функциональности
2. Внести изменения
3. Убедиться, что все тесты проходят
4. Создать Pull Request

## Лицензия

Copyright © 2025 ФГУП «ГОЗНАК». Все права защищены.
