# 📄 Manteq Document System

A **production-ready** document a### **Required Software**
- ✅ **.NET 9.0 SDK** - Download from Microsoft
- ✅ **SQL Server Express** with SQLEXPRESS instance  
- ✅ **LibreOffice** - For document conversion (free download)
- ✅ **Visual Studio Code** or Visual Studio 2022

### **🔒 Environment Configuration (Critical)**
**All services use environment variables for database connections - no credentials in code!**

Each service requires a `.env` file with database configuration:
```env
# Database Configuration Template
# Copy this to .env file in each service directory
DB_SERVER=YOUR_SERVER\\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev  
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true
```

**Required .env files:**
- `CMS.WebApi/.env`
- `TMS.WebApi/.env` 
- `EmailService.WebApi/.env`

### **🗂️ Shared Storage Setup**on platform consisting of **Content Management System (CMS)**, **Template Management System (TMS)**, and **Email Service** built with ASP.NET Core 9.0.

> 🔥 **Status**: ✅ **PRODUCTION READY** - Fully tested with shared storage architecture and microservices integration

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    MANTEQ DOCUMENT SYSTEM                   │
├─────────────────────────────────────────────────────────────┤
│  📁 CMS API          🎯 TMS API          📧 Email Service   │
│  localhost:5000      localhost:5267      localhost:5030     │
│                                                             │
│  • Document Storage  • Template Engine   • SMTP Integration │
│  • File Management   • Format Conversion • TMS Integration  │
│  • Database Access   • Placeholder Fill  • CMS Integration  │
└─────────────────────────────────────────────────────────────┘
                              │
                ┌─────────────┴─────────────┐
                │      SHARED RESOURCES     │
                ├───────────────────────────┤
                │ 🗄️ CmsDatabase_Dev       │
                │ 📁 C:\ManteqStorage_Shared │
                │ ⚙️ LibreOffice            │
                └───────────────────────────┘
```

### **🎯 Service Integration Flow**
```
User Upload Template → TMS → Creates Document in CMS → Stores in Shared Storage
Generate Document → TMS → Process Template → Save to TmsGenerated (auto-cleanup)
Send Email → EmailService → Call TMS → Generate HTML → Send via SMTP
```

## 🚀 Features Overview

### **📁 CMS (Content Management System) - Port 5000**
- ✅ **Document Storage**: Centralized file storage in shared directory
- ✅ **Database Integration**: SQL Server with Documents table
- ✅ **File Management**: Upload, download, and metadata tracking
- ✅ **REST API**: Complete CRUD operations with Swagger docs
- ✅ **Security**: File type validation and size limits

### **🎯 TMS (Template Management System) - Port 5267**
- ✅ **Office Template Processing**: Word, Excel, PowerPoint templates
- ✅ **Dynamic Placeholders**: Replace {{PropertyName}} with actual values
- ✅ **Multiple Export Formats**: Word, HTML, EmailHtml, PDF, Original
- ✅ **LibreOffice Integration**: High-quality document conversion
- ✅ **Auto-Cleanup**: Generated files removed every 15 minutes
- ✅ **CMS Integration**: Internal CMS services for template storage
- ✅ **Email-Ready HTML**: Base64 embedded images for email clients

### **📧 Email Service - Port 5030**
- ✅ **TMS Integration**: Generate documents on-the-fly for email content
- ✅ **CMS Integration**: Attach existing documents from CMS
- ✅ **SMTP Support**: Multi-account email configuration
- ✅ **EmailHtml Format**: Template content becomes email body
- ✅ **Health Monitoring**: Service status and configuration checks
- ✅ **Async Processing**: Non-blocking email operations

## 📋 Prerequisites

### **Required Software**
- ✅ **.NET 9.0 SDK** - Download from Microsoft
- ✅ **SQL Server Express** with SQLEXPRESS instance  
- ✅ **LibreOffice** - For document conversion (free download)
- ✅ **Visual Studio Code** or Visual Studio 2022

### **�️ Shared Storage Setup**
The system uses centralized file storage that all services share:

```powershell
# Create shared storage directories (run once)
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\CmsDocuments" -Force
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\TmsGenerated" -Force  
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\TmsTemp" -Force
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\EmailAttachments" -Force
```

### **🗄️ Database Requirements**
```sql
-- Single shared database for all services
CREATE DATABASE CmsDatabase_Dev;

-- Tables are created automatically via Entity Framework:
-- - Documents (CMS) - Stores all documents and templates
-- - Templates (TMS) - References Documents via foreign key
```

## ⚡ Quick Start (5 Minutes)

### **1. Clone and Setup**
```powershell
# Clone repository
git clone https://github.com/SalehShalab87/Manteq-doc-system.git
cd Manteq-doc-system

# Create shared storage (required)
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\CmsDocuments" -Force
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\TmsGenerated" -Force  
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\TmsTemp" -Force
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\EmailAttachments" -Force
```

### **2. Configure Environment Variables**
```powershell
# Create .env files for each service (REQUIRED)
@"
DB_SERVER=YOUR_SERVER\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev  
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true
"@ | Out-File -FilePath "CMS.WebApi\.env" -Encoding UTF8

@"
DB_SERVER=YOUR_SERVER\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev  
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true
"@ | Out-File -FilePath "TMS.WebApi\.env" -Encoding UTF8

@"
DB_SERVER=YOUR_SERVER\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev  
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true
"@ | Out-File -FilePath "EmailService.WebApi\.env" -Encoding UTF8
```

### **3. Build and Run Services**

# Build entire system
dotnet build ManteqDocumentSystem.sln
```

### **2. Start All Services (3 Terminals)**
```powershell
# Terminal 1: CMS (Content Management)
cd CMS.Webapi
dotnet run
# ➡️ Access: http://localhost:5000 (Swagger: http://localhost:5000/swagger)

# Terminal 2: TMS (Template Management) 
cd TMS.WebApi
dotnet run
# ➡️ Access: http://localhost:5267 (Swagger: http://localhost:5267/swagger)

# Terminal 3: Email Service
cd EmailService.WebApi
dotnet run
# ➡️ Access: http://localhost:5030 (Swagger: http://localhost:5030/swagger)
```

### **3. Test the System**
```powershell
# Health check all services
curl http://localhost:5000/api/documents/health
curl http://localhost:5267/api/templates/health  
curl http://localhost:5030/api/email/health

# ✅ All should return: {"status": "healthy"}
```

> **🎉 That's it!** All three services are running and ready to process documents.

## 🔗 API Endpoints

### **📁 CMS APIs (Port 5000)**
```http
# Upload and register documents
POST http://localhost:5000/api/documents/register
Content-Type: multipart/form-data

# Get document metadata  
GET http://localhost:5000/api/documents/{documentId}

# Download document file
GET http://localhost:5000/api/documents/{documentId}/download

# Health check
GET http://localhost:5000/api/documents/health
```

### **🎯 TMS APIs (Port 5267)**
```http
# Register template (creates document in CMS internally)
POST http://localhost:5267/api/templates/register
Content-Type: multipart/form-data

# Generate document from template
POST http://localhost:5267/api/templates/generate
Content-Type: application/json

# Download generated document
GET http://localhost:5267/api/templates/download/{generationId}

# Get template properties (placeholders)
GET http://localhost:5267/api/templates/{templateId}/properties

# Health check
GET http://localhost:5267/api/templates/health
```

### **📧 Email Service APIs (Port 5030)**  
```http
# Send email with TMS-generated content
POST http://localhost:5030/api/email/send-generated-document
Content-Type: application/json

# Send email with CMS document attachments
POST http://localhost:5030/api/email/send-with-attachments
Content-Type: application/json

# Health check
GET http://localhost:5030/api/email/health
```

## 🎯 Usage Examples

### **Example 1: Template Registration and Generation**
```http
# Step 1: Register a template (TMS calls CMS internally)
POST http://localhost:5267/api/templates/register
Content-Type: multipart/form-data

file: Email_Template.docx (contains {{CustomerName}}, {{PolicyNumber}})
name: Customer Email Template
description: Template for customer communications

# Response: { "templateId": "96cec0ae-a1f9-4e01-8e07-16ddd57b4b25" }

# Step 2: Generate document with data
POST http://localhost:5267/api/templates/generate
Content-Type: application/json

{
  "templateId": "96cec0ae-a1f9-4e01-8e07-16ddd57b4b25",
  "propertyValues": {
    "CustomerName": "John Smith",
    "PolicyNumber": "POL-2025-001234"
  },
  "exportFormat": "EmailHtml",
  "generatedBy": "API User"
}

# Response: { "generationId": "abc123...", "downloadUrl": "/api/templates/download/abc123..." }
```

### **Example 2: Email Automation**
```http
# Send email using TMS template (generates content on-the-fly)
POST http://localhost:5030/api/email/send-generated-document
Content-Type: application/json

{
  "to": ["john.smith@customer.com"],
  "subject": "Your Policy Documents",
  "templateId": "96cec0ae-a1f9-4e01-8e07-16ddd57b4b25",
  "propertyValues": {
    "CustomerName": "John Smith",
    "PolicyNumber": "POL-2025-001234",
    "SupportEmail": "support@manteq-me.com"
  },
  "exportFormat": "EmailHtml"
}

# The template content becomes the email body automatically!
```

### **Example 3: File Storage Verification**
```powershell
# Check where files are stored
Get-ChildItem "C:\ManteqStorage_Shared\CmsDocuments\"        # Original templates
Get-ChildItem "C:\ManteqStorage_Shared\TmsGenerated\"        # Generated documents (auto-cleanup)
Get-ChildItem "C:\ManteqStorage_Shared\TmsTemp\"             # Processing workspace
```

### **🎨 Export Formats**
- 📄 **Word** (.docx) - Preserve original formatting and layout
- 🌐 **HTML** - Web-compatible output with CSS styling  
- 📧 **EmailHtml** - Email-optimized HTML with base64 embedded images
- 📋 **PDF** - Professional document format (via LibreOffice)
- 📂 **Original** - Keep source format unchanged

### **📧 EmailHtml Special Features**
- 🖼️ **Base64 Images**: All images embedded directly (no external references)
- 🎨 **Email-Client Compatible**: Works with Outlook, Gmail, etc.
- 🧹 **Clean HTML**: LibreOffice field codes removed automatically
- 📱 **Responsive**: Mobile-friendly email layouts

## 🔧 Configuration

### **� Environment Variables (All Services)**
**Database connections are configured via `.env` files in each service directory - NO credentials in appsettings.json!**

**Required `.env` files:**
- `CMS.WebApi/.env`
- `TMS.WebApi/.env` 
- `EmailService.WebApi/.env`

**Template for each `.env` file:**
```env
DB_SERVER=YOUR_SERVER\\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev  
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true
```

### **📁 CMS Settings** (`CMS.WebApi/appsettings.json`)
```json
{
  "FileStorage": {
    "Path": "C:\\ManteqStorage_Shared\\CmsDocuments"  // Permanent document storage
  }
}
```

### **🎯 TMS Settings** (`TMS.WebApi/appsettings.json`)
```json
{
  "FileStorage": {
    "Path": "C:\\ManteqStorage_Shared\\CmsDocuments"  // For internal CMS services
  },
  "TMS": {
    "DocumentRetentionHours": 0.25,        // 15 minutes for generated files
    "CleanupIntervalMinutes": 5,           // Cleanup every 5 minutes
    "MaxFileSizeMB": 100,                  // 100MB file limit
    "AllowedFileTypes": [".docx", ".xlsx", ".pptx"],
    "LibreOfficeTimeout": 30000,           // 30 seconds
    "SharedStoragePath": "C:\\ManteqStorage_Shared\\TmsGenerated",  // Generated docs
    "TempUploadPath": "C:\\ManteqStorage_Shared\\TmsTemp"           // Working directory
  }
}
```

### **📧 Email Service Settings** (`EmailService.WebApi/appsettings.json`)
```json
{
  "EmailSettings": {
    "DefaultFromEmail": "noreply@manteq-me.com",
    "DefaultFromName": "Manteq System"
  }
}
```

### **🧹 Auto-Cleanup Behavior**
- **Templates**: ❌ **NEVER** cleaned up - stored permanently in CmsDocuments
- **Generated Documents**: ✅ **Auto-cleanup** every 5 minutes - removed after 15 minutes
- **Temp Files**: ✅ **Immediate cleanup** after processing

## 🗂️ Project Structure

```
Manteq-doc-system/
├── 📄 ManteqDocumentSystem.sln    # Main solution (CMS + TMS + Email)
├── 📄 README.md                   # This file (system overview)
├── 📄 TEAM_GUIDE.md               # 🆕 Comprehensive developer guide
├── 📄 LICENSE.txt
├── 
├── 📁 CMS.WebApi/                 # Content Management System (Port 5000)
│   ├── Controllers/               # REST API endpoints
│   ├── Data/                      # Entity Framework DbContext
│   ├── Models/                    # Document models and DTOs
│   ├── Services/                  # File storage and business logic
│   ├── appsettings.json          # Database and storage configuration
│   └── README.md                  # CMS-specific documentation
├── 
├── 📁 TMS.WebApi/                 # Template Management System (Port 5267)
│   ├── TMS.WebApi.sln            # TMS solution with CMS dependency
│   ├── Controllers/               # Template processing endpoints
│   ├── Services/                  # Document generation and embedding
│   ├── Models/                    # Template models and requests
│   ├── Infrastructure/            # Controller filtering for CMS
│   ├── appsettings.json          # TMS and storage configuration  
│   └── README.md                  # TMS-specific documentation
├── 
└── 📁 EmailService.WebApi/        # Email Service (Port 5030)
    ├── EmailService.WebApi.sln    # Email solution with TMS+CMS dependencies
    ├── Controllers/               # Email sending endpoints
    ├── Services/                  # TMS/CMS integration services
    ├── Models/                    # Email models and requests
    ├── appsettings.json          # Email configuration
    └── README.md                  # Email service documentation
```

### **🗄️ Database Schema**
```sql
CmsDatabase_Dev
├── Documents (CMS)               # All documents and templates
│   ├── Id (PK)                   # Document identifier
│   ├── FileName                  # Original file name
│   ├── FilePath                  # Storage location
│   ├── Description               # User description
│   └── CreatedAt                 # Upload timestamp
└── Templates (TMS)               # Template metadata
    ├── Id (PK)                   # Template identifier  
    ├── CmsDocumentId (FK)        # References Documents.Id
    ├── Name                      # Template name
    ├── Description               # Template description
    └── CreatedAt                 # Registration timestamp
```

### **📁 File Storage Layout**
```
C:\ManteqStorage_Shared\
├── CmsDocuments\                 # 📁 PERMANENT storage (CMS + TMS templates)
│   ├── email-doc-test_xyz.docx   # Direct CMS uploads
│   └── Template_abc123.docx      # TMS registered templates
├── TmsGenerated\                 # 🎯 TEMPORARY storage (15min retention)
│   ├── generated_xyz.html        # EmailHtml output
│   └── generated_abc.pdf         # PDF conversions
├── TmsTemp\                      # 🔄 WORKING directory (immediate cleanup)
│   └── temp_processing_files     # During generation only
└── EmailAttachments\             # 📧 EMAIL storage (future use)
    └── attachment_files          # Email service files
```

## 🧪 Testing the System

### **🔍 Health Checks**
```powershell
# Verify all services are running
curl http://localhost:5000/api/documents/health   # CMS
curl http://localhost:5267/api/templates/health   # TMS  
curl http://localhost:5030/api/email/health       # Email Service

# All should return: {"status": "healthy", "service": "ServiceName"}
```

### **📋 Complete Workflow Test**
```powershell
# Step 1: Register a template (TMS → CMS internally)
curl -X POST "http://localhost:5267/api/templates/register" `
     -F "file=@Email_Template.docx" `
     -F "name=Test Template" `
     -F "description=Test template for workflow"

# Step 2: Generate document
curl -X POST "http://localhost:5267/api/templates/generate" `
     -H "Content-Type: application/json" `
     -d '{"templateId":"your-template-id","propertyValues":{"CustomerName":"Test User"},"exportFormat":"EmailHtml"}'

# Step 3: Send email with generated content
curl -X POST "http://localhost:5030/api/email/send-generated-document" `
     -H "Content-Type: application/json" `
     -d '{"to":["test@example.com"],"subject":"Test Email","templateId":"your-template-id","propertyValues":{"CustomerName":"Test User"}}'
```

### **🌐 Swagger UI Access**
- **CMS**: http://localhost:5000/swagger
- **TMS**: http://localhost:5267/swagger  
- **Email**: http://localhost:5030/swagger

## 🚀 Deployment and Scaling

### **Development Environment**
```powershell
# Run all services locally
dotnet run --project CMS.WebApi
dotnet run --project TMS.WebApi  
dotnet run --project EmailService.WebApi
```

### **Production Build**
```powershell
# Build optimized releases
dotnet publish CMS.WebApi -c Release -o ./publish/cms
dotnet publish TMS.WebApi -c Release -o ./publish/tms  
dotnet publish EmailService.WebApi -c Release -o ./publish/email
```

### **⚡ Performance Features**
- 🧹 **Auto-cleanup**: Generated files removed every 15 minutes
- 💾 **Memory efficient**: Shared storage with minimal memory footprint  
- 🔄 **Async processing**: Non-blocking document generation and email sending
- ⚖️ **Load balancing ready**: Stateless services can scale horizontally
- 📊 **Configurable timeouts**: LibreOffice process management and cleanup

### **🔧 Production Considerations**
- **Database**: Upgrade to SQL Server Standard/Enterprise for production
- **Storage**: Consider Azure Blob Storage or NAS for shared file storage  
- **Monitoring**: Implement health checks and performance monitoring
- **Security**: Add authentication, authorization, and HTTPS certificates
- **Backup**: Regular database and file storage backups

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

## ❌ Troubleshooting

### **🔌 Service Connection Issues**
```powershell
# Check if services are running
netstat -ano | findstr ":5000"    # CMS
netstat -ano | findstr ":5267"    # TMS
netstat -ano | findstr ":5030"    # Email Service

# Kill processes if needed
taskkill /PID <process-id> /F
```

### **🗄️ Database Connection Problems**
```sql
-- Verify SQL Server is running
SELECT @@VERSION

-- Check database exists
USE master
SELECT name FROM sys.databases WHERE name = 'CmsDatabase_Dev'

-- If database is missing, it will be created automatically on first startup
```

### **📁 File Storage Issues**
```powershell
# Verify storage directories exist and have correct permissions
Test-Path "C:\ManteqStorage_Shared\CmsDocuments"
Test-Path "C:\ManteqStorage_Shared\TmsGenerated"
Get-Acl "C:\ManteqStorage_Shared"

# Recreate if missing
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\CmsDocuments" -Force
```

### **⚙️ LibreOffice Issues (TMS)**
```powershell
# Check LibreOffice installation
Test-Path "C:\Program Files\LibreOffice\program\soffice.exe"
Test-Path "C:\Program Files (x86)\LibreOffice\program\soffice.exe"

# Download from: https://www.libreoffice.org/download/download/
```

### **🔍 Common Error Messages**
- **"Instance failure"** → SQL Server not running or wrong connection string
- **"Directory not found"** → Run storage setup commands above
- **"Template not found"** → Template ID invalid or cleanup removed generated file
- **"LibreOffice timeout"** → LibreOffice not installed or process hanging

## � Additional Resources

### **📖 Documentation**
- 📄 **[TEAM_GUIDE.md](TEAM_GUIDE.md)** - Comprehensive developer guide with workflows and examples
- 📁 **[CMS README](CMS.WebApi/README.md)** - Content Management System documentation
- 🎯 **[TMS README](TMS.WebApi/README.md)** - Template Management System documentation  
- 📧 **[Email Service README](EmailService.WebApi/README.md)** - Email Service documentation

### **🌐 API Documentation**
- **CMS Swagger**: http://localhost:5000/swagger
- **TMS Swagger**: http://localhost:5267/swagger
- **Email Swagger**: http://localhost:5030/swagger

### **🛠️ Development Tools**
- **Visual Studio Code** with C# extension
- **Postman** or **curl** for API testing
- **SQL Server Management Studio** for database management
- **LibreOffice** for document conversion testing

---

## 📞 Support

- **👨‍💻 Lead Developer**: Saleh Shalab
- **📧 Email**: salehshalab2@gmail.com  
- **🌐 Repository**: https://github.com/SalehShalab87/Manteq-doc-system
- **🐛 Issues**: Use GitHub Issues for bug reports and feature requests

---

## 🎉 Status: Production Ready

✅ **All services fully tested and operational**  
✅ **Shared storage architecture implemented**  
✅ **Database schema stable and optimized**  
✅ **Error handling and logging comprehensive**  
✅ **API documentation complete**  
✅ **Auto-cleanup and performance optimized**

**The Manteq Document System is ready for production deployment and provides a complete document automation solution with professional-grade template processing, multiple export formats, email automation, and seamless microservice integration.**

🚀 **Happy document processing!**