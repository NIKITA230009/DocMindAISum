# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем файлы проектов
COPY DocMind.Core/DocMind.Core.csproj DocMind.Core/
COPY DocMind.Data/DocMind.Data.csproj DocMind.Data/
COPY DocMind.Desktop/DocMind.Desktop.csproj DocMind.Desktop/
COPY DocMind.Infrastructure/DocMind.Infrastructure.csproj DocMind.Infrastructure/
COPY DocMind.Services/DocMind.Services.csproj DocMind.Services/
COPY DocMind.Tests/DocMind.Tests.csproj DocMind.Tests/

# Восстанавливаем зависимости
RUN dotnet restore DocMind.Desktop/DocMind.Desktop.csproj

# Копируем исходный код
COPY DocMind.Core/ DocMind.Core/
COPY DocMind.Data/ DocMind.Data/
COPY DocMind.Desktop/ DocMind.Desktop/
COPY DocMind.Infrastructure/ DocMind.Infrastructure/
COPY DocMind.Services/ DocMind.Services/
COPY DocMind.Tests/ DocMind.Tests/

# Публикуем приложение
RUN dotnet publish DocMind.Desktop/DocMind.Desktop.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY samples/ /app/samples/

# Установка зависимостей для Avalonia и X11
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgl1 \
    libx11-xcb1 \
    libxcb1 \
    libxcb-icccm4 \
    libxcb-image0 \
    libxcb-keysyms1 \
    libxcb-randr0 \
    libxcb-render-util0 \
    libxcb-shape0 \
    libxcb-xfixes0 \
    libxcb-xinerama0 \
    libfontconfig1 \
    libfreetype6 \
    libxkbcommon-x11-0 \
    libxrandr2 \
    libice6 \
    libsm6 \
    libxtst6 \
    libxrender1 \
    && rm -rf /var/lib/apt/lists/*

# Копируем приложение из build-стадии
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DocMind.Desktop.dll"]