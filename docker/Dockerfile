FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/KazoOCR.Docker/KazoOCR.Docker.csproj", "src/KazoOCR.Docker/"]
COPY ["src/KazoOCR.Core/KazoOCR.Core.csproj", "src/KazoOCR.Core/"]
RUN dotnet restore "src/KazoOCR.Docker/KazoOCR.Docker.csproj"

COPY . .
RUN dotnet publish "src/KazoOCR.Docker/KazoOCR.Docker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends ocrmypdf tesseract-ocr-fra unpaper \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "KazoOCR.Docker.dll"]
