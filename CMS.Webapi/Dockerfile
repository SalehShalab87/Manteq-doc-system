# CMS Web API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install curl for healthchecks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ManteqDocumentSystem.sln", "./"]
COPY ["CMS.Webapi/CMS.WebApi.csproj", "CMS.Webapi/"]
RUN dotnet restore "CMS.Webapi/CMS.WebApi.csproj"

# Copy all source code
COPY . .

# Build CMS project
WORKDIR "/src/CMS.Webapi"
RUN dotnet build "CMS.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CMS.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create storage directories
RUN mkdir -p /app/storage/CmsDocuments

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CMS.WebApi.dll"]
