# Email Service Web API Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5030

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ManteqDocumentSystem.sln", "./"]
COPY ["CMS.Webapi/CMS.WebApi.csproj", "CMS.Webapi/"]
COPY ["TMS.WebApi/TMS.WebApi.csproj", "TMS.WebApi/"]
COPY ["EmailService.WebApi/EmailService.WebApi.csproj", "EmailService.WebApi/"]
RUN dotnet restore "EmailService.WebApi/EmailService.WebApi.csproj"

# Copy all source code
COPY . .

# Build Email Service project
WORKDIR "/src/EmailService.WebApi"
RUN dotnet build "EmailService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
# Publish Email Service - this will include both CMS.WebApi.dll and TMS.WebApi.dll as dependencies
RUN dotnet publish "EmailService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:CopyLocalLockFileAssemblies=true

FROM base AS final
WORKDIR /app

# Create storage directories
RUN mkdir -p /app/storage/EmailAttachments

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5030
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EmailService.WebApi.dll"]
