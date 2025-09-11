# 📄 Manteq Document System

A comprehensive document automation platform consisting of **Content Management System (CMS)**, **Template Management System (TMS)**, and **Email Service** built with ASP.NET Core 9.0.

## 🏗️ System Architecture

```
Manteq Document System
├── 📁 CMS.WebApi/              # Content Management System
│   ├── Document storage & retrieval
│   ├── File management services
│   └── Database integration
├── 📁 TMS.WebApi/              # Template Management System  
│   ├── Template processing
│   ├── Document generation
│   ├── Multiple export formats
│   └── Auto-download functionality
└── 📁 EmailService.WebApi/     # Email Service
    ├── TMS/CMS integration
    ├── Template-based email content
    ├── Document attachments
    └── Multi-account SMTP support
```

## 🚀 Features Overview

### **CMS (Content Management System)**
- ✅ Document registration and storage
- ✅ File metadata management
- ✅ RESTful API with Swagger documentation
- ✅ SQL Server integration
- ✅ Multi-format file support

### **TMS (Template Management System)**
- ✅ Office template processing (Word, Excel, PowerPoint)
- ✅ Dynamic placeholder replacement
- ✅ Multiple export formats (Word, HTML, Email HTML, PDF)
- ✅ Auto-download functionality
- ✅ Document embedding and composition
- ✅ LibreOffice integration for high-quality conversion
- ✅ Email-friendly HTML with base64 embedded images
- ✅ Configurable cleanup and retention

### **Email Service**
- ✅ TMS template-based email generation
- ✅ CMS document attachments
- ✅ Multi-account SMTP configuration
- ✅ EmailHtml format integration (replaces body content)
- ✅ Auto-cleanup of generated documents
- ✅ Email validation and error handling

## 📋 Prerequisites

- .NET 9.0 SDK
- SQL Server Express (SQLEXPRESS instance)
- LibreOffice (for TMS document conversion)
- Visual Studio Code or Visual Studio

### 🔧 **Database Setup Requirements**
```sql
-- Create the shared database for all services
CREATE DATABASE CmsDatabase_Dev;
```

### 🔒 **Security Configuration**
Each service requires a `.env` file with environment variables. **Never commit these files to version control.**

## ⚙️ Quick Start

### 1. **Environment Setup**
Create `.env` files in each service directory:

```bash
# Copy templates and configure with your settings
cp CMS.Webapi/.env.template CMS.Webapi/.env
cp TMS.WebApi/.env.template TMS.WebApi/.env  
cp EmailService.WebApi/.env.template EmailService.WebApi/.env

# Edit each .env file with your:
# - Database server details
# - SMTP credentials (for EmailService)
```

### 2. **Database Setup**
```sql
-- Create database (or configure connection string)
CREATE DATABASE CmsDatabase_Dev;
```

### 3. **Build Options**
```bash
# Option 1: Build entire system
dotnet build ManteqDocumentSystem.sln

# Option 2: Build TMS with dependencies
cd TMS.WebApi
dotnet build TMS.WebApi.sln

# Option 3: Build Email Service with dependencies
cd EmailService.WebApi  
dotnet build EmailService.WebApi.sln

# Option 4: Build individual projects
cd CMS.WebApi && dotnet build
cd TMS.WebApi && dotnet build
cd EmailService.WebApi && dotnet build
```

### 4. **Run CMS API**
```bash
cd CMS.Webapi
dotnet run
# Access: http://localhost:5077 (Swagger: https://localhost:7276)
```

### 5. **Run TMS API**
```bash
cd TMS.WebApi
dotnet run
# Access: http://localhost:5267 (Swagger: http://localhost:5267)
```

### 6. **Run Email Service**
```bash
cd EmailService.WebApi
dotnet run
# Access: http://localhost:5030 (Swagger: http://localhost:5030)
```

> **🔒 Security Note**: All services use environment variables for database connections and sensitive configuration.

## 🔗 API Endpoints

### **CMS APIs**
- `POST /api/documents/register` - Upload and register documents
- `GET /api/documents/{id}` - Retrieve document metadata
- `GET /api/documents/{id}/download` - Download document files

### **TMS APIs**
- `POST /api/templates/register` - Register new templates
- `GET /api/templates/{id}` - Retrieve template metadata
- `GET /api/templates/{id}/properties` - Get template properties
- `POST /api/templates/generate` - Generate documents from templates
- `POST /api/templates/generate-with-embeddings` - Advanced template composition
- `GET /api/templates/download/{id}` - Download generated documents

### **Email Service APIs**
- `POST /api/email/send-with-template` - Send email with TMS template content
- `POST /api/email/send-with-documents` - Send email with CMS document attachments
- `GET /api/email/accounts` - Get configured email accounts
- `GET /api/email/health` - Service health check

## 🎯 Key Capabilities

### **Document Generation**
```http
POST /api/templates/generate?autoDownload=true
Content-Type: application/json

{
  "templateId": "your-template-id",
  "propertyValues": {
    "CustomerName": "John Doe",
    "PolicyNumber": "POL-123456"
  },
  "exportFormat": "EmailHtml",
  "generatedBy": "API User"
}
```

### **Email Automation**
```http
POST /api/email/send-with-template
Content-Type: application/json

{
  "toRecipients": ["customer@example.com"],
  "subject": "Your Invoice Document",
  "templateId": "your-template-id",
  "propertyValues": {
    "CustomerName": "John Doe", 
    "InvoiceNumber": "INV-123456"
  },
  "exportFormat": "EmailHtml"
}
```

### **Export Formats**
- 📄 **Word** (.docx) - Preserve original formatting
- 🌐 **HTML** - Web-compatible output
- 📧 **Email HTML** - Email-optimized with embedded images
- 📋 **PDF** - Professional document format
- 📂 **Original** - Keep source format

### **Email HTML Features**
- 🖼️ Base64 embedded images (no external files)
- 🎨 Email-client compatible styling
- 🧹 LibreOffice field cleanup
- 📱 Responsive design elements

## 🔧 Configuration

### **TMS Settings** (`appsettings.json`)
```json
{
  "TMS": {
    "DocumentRetentionHours": 0.0167,    // 1 minute
    "CleanupIntervalMinutes": 1,         // Every minute
    "MaxFileSizeMB": 100,                // 100MB limit
    "AllowedFileTypes": [".docx", ".xlsx", ".pptx"],
    "LibreOfficeTimeout": 30000          // 30 seconds
  }
}
```

## 🗂️ Project Structure

```
Manteq-doc-system/
├── ManteqDocumentSystem.sln    # Main solution (CMS + TMS + Email)
├── CMS.WebApi/                 # Content Management System
│   ├── Controllers/
│   ├── Data/
│   ├── Models/
│   ├── Services/
│   └── README.md
├── TMS.WebApi/                 # Template Management System
│   ├── TMS.WebApi.sln          # TMS solution (TMS + CMS dependency)
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Infrastructure/
│   ├── GeneratedDocuments/     # Auto-cleanup directory
│   └── README.md
├── EmailService.WebApi/        # Email Service
│   ├── EmailService.WebApi.sln # Email solution (Email + TMS + CMS)
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── Infrastructure/         # Controller filtering
│   ├── .env.template          # SMTP configuration template
│   └── README.md
├── .gitignore                  # Ignores generated files
├── SYSTEM_OVERVIEW.md          # Complete system documentation
└── README.md                   # This file
```

### **Solution Files**
- **`ManteqDocumentSystem.sln`** - Complete system build (all services)
- **`TMS.WebApi/TMS.WebApi.sln`** - TMS-focused build with CMS dependency
- **`EmailService.WebApi/EmailService.WebApi.sln`** - Email Service with TMS+CMS dependencies

## 🧪 Testing

### **Swagger UI Access**
- **CMS**: https://localhost:7276
- **TMS**: http://localhost:5267
- **Email Service**: http://localhost:5030

### **Sample Workflow**
1. **Upload template** via CMS
2. **Register template** in TMS
3. **Generate document** with custom data
4. **Send via Email Service** or **Auto-download**

## 🚀 Deployment

### **Development**
```bash
# Start all systems
dotnet run --project CMS.WebApi
dotnet run --project TMS.WebApi
dotnet run --project EmailService.WebApi
```

### **Production**
```bash
# Build for production
dotnet publish CMS.WebApi -c Release
dotnet publish TMS.WebApi -c Release
dotnet publish EmailService.WebApi -c Release
```

## 📈 Performance Features

- ⚡ **Auto-cleanup**: Generated files removed every minute
- 💾 **Memory efficient**: Temporary file management
- 🔄 **Async processing**: Non-blocking operations
- 📊 **Configurable timeouts**: LibreOffice process management

## 🛠️ Integration

### **Use TMS as Service**
```csharp
// In your project
services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
services.AddScoped<ITemplateService, TemplateService>();
```

### **CMS Integration**
TMS uses CMS services internally for document storage while exposing only TMS-specific endpoints.

## � Troubleshooting

### **Database Connection Issues**
If you get "Instance failure" errors:

1. **Check SQL Server Status**:
   ```powershell
   Get-Service -Name "*SQL*" | Where-Object {$_.Status -eq "Running"}
   ```

2. **For Local SQLEXPRESS**: Services automatically use named pipes for better reliability
3. **For Remote Servers**: Ensure SQL Server Browser service is running
4. **Connection String**: Verify your `.env` file has correct database server name

### **Missing Environment Variables**
```bash
# Ensure .env files exist and contain:
DB_SERVER=YOUR_SERVER\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true

# For EmailService, also add:
SMTP_HOST=your-smtp-host
SMTP_PORT=587
SMTP_USERNAME=your-email@domain.com
SMTP_PASSWORD=your-password
```

### **Port Conflicts**
Default ports:
- CMS: 5077 (HTTP), 7276 (HTTPS)  
- TMS: 5267 (HTTP)
- EmailService: 5030 (HTTP)

Change in `Properties/launchSettings.json` if needed.

## �📝 Development Notes

- **Architecture**: Clean separation between CMS (storage) and TMS (processing)
- **Security**: Controller exclusion prevents exposing internal CMS endpoints
- **Scalability**: Configurable cleanup and retention policies
- **Maintainability**: Comprehensive logging and error handling

---

## 📞 Support

- **Author**: Saleh Shalab
- **Email**: salehshalab2@gmail.com
- **Repository**: https://github.com/SalehShalab87/Manteq-doc-system

**Status**: ✅ **Production Ready** - CMS, TMS, and Email Service are fully functional and tested.

The system provides a complete document automation solution with professional-grade template processing, multiple export formats, email automation, and seamless integration capabilities.