# ==============================================================================
# Stage 1: Build all projects
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["src/KazoOCR.Core/KazoOCR.Core.csproj", "src/KazoOCR.Core/"]
COPY ["src/KazoOCR.Api/KazoOCR.Api.csproj", "src/KazoOCR.Api/"]
COPY ["src/KazoOCR.Web/KazoOCR.Web.csproj", "src/KazoOCR.Web/"]
COPY ["src/KazoOCR.Docker/KazoOCR.Docker.csproj", "src/KazoOCR.Docker/"]

# Restore all projects
RUN dotnet restore "src/KazoOCR.Api/KazoOCR.Api.csproj"
RUN dotnet restore "src/KazoOCR.Web/KazoOCR.Web.csproj"
RUN dotnet restore "src/KazoOCR.Docker/KazoOCR.Docker.csproj"

# Copy all source
COPY . .

# Publish each project
RUN dotnet publish "src/KazoOCR.Api/KazoOCR.Api.csproj" -c Release -o /app/api /p:UseAppHost=false
RUN dotnet publish "src/KazoOCR.Web/KazoOCR.Web.csproj" -c Release -o /app/web /p:UseAppHost=false
RUN dotnet publish "src/KazoOCR.Docker/KazoOCR.Docker.csproj" -c Release -o /app/worker /p:UseAppHost=false

# ==============================================================================
# Stage 2: API runtime
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
RUN apt-get update \
    && apt-get install -y --no-install-recommends ocrmypdf tesseract-ocr-fra tesseract-ocr-eng unpaper \
    && groupadd --system kazoocr \
    && useradd --system --gid kazoocr --create-home --home-dir /home/kazoocr kazoocr \
    && mkdir -p /data /app/data \
    && chown -R kazoocr:kazoocr /data /app/data \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/api ./
RUN chown -R kazoocr:kazoocr /app

USER kazoocr
EXPOSE 5000
ENTRYPOINT ["dotnet", "KazoOCR.Api.dll"]

# ==============================================================================
# Stage 3: Web runtime
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS web
RUN groupadd --system kazoocr \
    && useradd --system --gid kazoocr --create-home --home-dir /home/kazoocr kazoocr

WORKDIR /app
COPY --from=build /app/web ./
RUN chown -R kazoocr:kazoocr /app

USER kazoocr
EXPOSE 5001
ENTRYPOINT ["dotnet", "KazoOCR.Web.dll"]

# ==============================================================================
# Stage 4: Worker runtime (legacy, standalone)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS worker
RUN apt-get update \
    && apt-get install -y --no-install-recommends ocrmypdf tesseract-ocr-fra tesseract-ocr-eng unpaper \
    && groupadd --system kazoocr \
    && useradd --system --gid kazoocr --create-home --home-dir /home/kazoocr kazoocr \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/worker ./
RUN chown -R kazoocr:kazoocr /app

USER kazoocr
ENTRYPOINT ["dotnet", "KazoOCR.Docker.dll"]
