DocMind AI

Интеллектуальный ассистент для работы с документами Word на базе локальных AI-моделей
DocMind AI — это десктопное приложение для анализа, суммаризации и перефразирования текста в документах Microsoft Word (.docx) с использованием локальных языковых моделей (LLM). Все данные обрабатываются на вашем устройстве без отправки в облако.

Возможности:

- Открытие документов
Загрузка и чтение файлов .docx через Open XML SDK
- Сохранение документов
Редактирование и сохранение с сохранением структуры Word
- Суммаризация
Краткое изложение содержания документа через AI
- Перефразирование
Улучшение стиля и грамматики текста через AI
- Кэширование
Автоматическое кэширование документов для ускорения работы
- Приватность
Вся обработка происходит локально, без отправки данных в интернет

Технологический стек:
NET 10.0
UI Framework
Avalonia UI (кроссплатформенный)
AI Inference
LLamaSharp (Phi-3-mini-4k-instruct-q4.gguf)
DocumentFormat.OpenXml
LiteDB
Refit

Архитектура:
MVVM

Структура проекта:
DocMind/
├── DocMind.Core/           # Общие модели, DTO, интерфейсы
│   ├── Dto/                # Request/Response модели
│   ├── Interfaces/         # Интерфейсы сервисов
│   ├── Models/             # Бизнес-модели
│   └── DocMind.Core.csproj
├── DocMind.Desktop/        # Десктопное приложение (Avalonia)
│   ├── ViewModels/         # MVVM ViewModel
│   ├── Views/              # XAML представления
│   ├── Assets/             # Иконки, ресурсы
│   └── DocMind.Desktop.csproj
├── DocMind.Services/       # Бизнес-логика и сервисы
│   ├── Services/           # Реализации сервисов
│   └── DocMind.Services.csproj
├── DocMind.Tests/          # Юнит-тесты
│   └── DocMind.Tests.csproj
├── DocMind.slnx            # Решение
├── .gitignore              # Git ignore правила
└── README.md

Требования:
.NET SDK
10.0+
https://dotnet.microsoft.com/download
AI Модель
Phi-3-mini-4k-instruct-q4.gguf
https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf

Установка
1. Клонировать репозиторий
git clone https://github.com/NIKITA230009/DocMindAISum.git
cd DocMindAISum

2. Скачать AI модель
Поместите файл Phi-3-mini-4k-instruct-q4.gguf в папку:
DocMind.Desktop/Phi-3-mini-4k-instruct-q4.gguf

3. Восстановить зависимости
dotnet restore

4. Собрать проект
dotnet build

5. Запустить приложение
dotnet run --project DocMind.Desktop

Расположение модели:
AI модель должна находиться в папке приложения:
DocMind.Desktop/
└── Phi-3-mini-4k-instruct-q4.gguf


Параметры инференса
Настроены в LLamaSharpAiService.cs:

Параметр    Значение Описание 
ContextSize 2048     Размер контекста модели 
MaxTokens   512      Максимальная длина ответа 
Temperature 0.2      Креативность (меньше = точнее)
TopP        0.85     Выборка токенов 

Локальное хранилище
Данные LiteDB сохраняются в:


%LOCALAPPDATA%\DocMind\
├── documents.db    # Кэш документов
└── history.db      # История запросов

Как использовать

1. Открыть документ

1. Нажмите кнопку «Открыть»
2. Выберите файл .docx
3. Текст загрузится в редактор

2. Суммаризировать текст

1. Нажмите кнопку «Суммаризировать»
2. Дождитесь обработки AI
3. Результат появится в панели AI
4. Нажмите «Вставить результат» для добавления в документ

3. Перефразировать текст

1. Выделите текст в редакторе
2. Нажмите кнопку «Перефразировать»
3. AI улучшит стиль и грамматику
4. Вставьте результат в документ

4. История документов

- Левая панель показывает недавние документы
- Клик по документу открывает его
- История хранится в LiteDB


Тестирование

Запустить все тесты
dotnet test

Запустить тесты с покрытием
dotnet test --collect:"XPlat Code Coverage"


Зависимости

NuGet пакеты

| Пакет | Версия | Назначение |
| LiteDB | 5.0.21 | Локальная база данных |
| Refit | 10.0.1 | HTTP API клиент |
| LLamaSharp | * | Локальный AI инференс |
| DocumentFormat.OpenXml | * | Работа с Word |
| Avalonia | * | UI фреймворк |

Безопасность
Данные 
Вся обработка локально, без отправки в облако 
Модель
Локальный файл .gguf, не требует интернета
Кэш
LiteDB в `%LOCALAPPDATA%`, доступ только пользователю 
История
Хранится локально, можно очистить через UI


Вклад в проект

1. Форкнуть репозиторий
2. Создать ветку
git checkout -b feature/my-feature
3. Внести изменения и закоммитить
git commit -m "Add my feature"
4. Отправить пулл-реквест
git push origin feature/my-feature

Лицензия

MIT License — см. файл [LICENSE](LICENSE) для деталей.

Контакты

Репозиторий https://github.com/NIKITA230009/DocMindAISum
Автор NIKITA230009



DocMind AI — ваш локальный интеллектуальный помощник для работы с документами
⭐ Если проект полезен — поставьте звезду на GitHub!
</div>
