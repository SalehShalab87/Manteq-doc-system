# Email Service Web API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install curl for healthchecks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 5030

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ManteqDocumentSystem.sln", "./"]
COPY ["EmailService.WebApi/EmailService.WebApi.csproj", "EmailService.WebApi/"]

# Restore EmailService project (no CMS/TMS dependencies)
RUN dotnet restore "EmailService.WebApi/EmailService.WebApi.csproj"

# Copy all source code
COPY . .

# Build Email Service project
WORKDIR "/src/EmailService.WebApi"
RUN dotnet build "EmailService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5030
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EmailService.WebApi.dll"]
