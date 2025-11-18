# TMS Web API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install LibreOffice for document conversion
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libreoffice \
    libreoffice-writer \
    libreoffice-calc \
    libreoffice-impress \
    fonts-liberation \
    fonts-dejavu \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* \
    && echo "LibreOffice installed at:" \
    && which libreoffice \
    && which soffice \
    && ls -la /usr/bin/libreoffice* /usr/bin/soffice* || true

WORKDIR /app
EXPOSE 5267

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ManteqDocumentSystem.sln", "./"]
COPY ["CMS.Webapi/CMS.WebApi.csproj", "CMS.Webapi/"]
COPY ["TMS.WebApi/TMS.WebApi.csproj", "TMS.WebApi/"]

# Restore both projects (TMS depends on CMS)
RUN dotnet restore "TMS.WebApi/TMS.WebApi.csproj"

# Copy all source code
COPY . .

# Build TMS project
WORKDIR "/src/TMS.WebApi"
RUN dotnet build "TMS.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TMS.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create storage directories
RUN mkdir -p /app/storage/CmsDocuments && \
    mkdir -p /app/storage/TmsGenerated && \
    mkdir -p /app/storage/TmsTemp

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5267
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TMS.WebApi.dll"]
