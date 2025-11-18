# 🎯 TMS Web API - Template Management System

**Stateless document generation engine** that transforms Office templates into dynamic documents with multiple export formats. Built with ASP.NET Core 9.0, OpenXML, and LibreOffice.

> 🎯 **Role**: Intelligent template processor that extracts placeholders, fills data, and converts documents to multiple formats including **EmailHtml** for email integration.

---

## 🏗️ Architecture Role

TMS operates as a **Stateless Service** in the microservices architecture:

```
┌─────────────────────────────────────────────────────────┐
│                MANTEQ DOCUMENT SYSTEM                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📧 Email Service ────► 🎯 TMS API ────► 📁 CMS API    │
│  (Stateless)           (Stateless)      (Data Gateway)  │
│                                                         │
│  Requests EmailHtml    • Template       • Database     │
│  for email body        • LibreOffice    • File Storage │
│                        • OpenXML        • REST API     │
│                        • HTTP Client                    │
└─────────────────────────────────────────────────────────┘
                            │
                ┌───────────┴────────────┐
                │    TMS OPERATIONS      │
                ├────────────────────────┤
                │ 📝 Extract Properties  │
                │ 🔄 Fill Placeholders   │
                │ 📄 Convert Formats     │
                │ 🧹 Auto-Cleanup (15min)│
                └────────────────────────┘
```

### **Stateless Design**
- ✅ **No Database**: All data via CMS HTTP API
- ✅ **HTTP Client**: Communicates with CMS for templates
- ✅ **Temporary Storage**: Generated files auto-cleaned
- ✅ **Horizontal Scaling**: Can run multiple instances

---

## 🚀 Key Features

### **📝 Template Processing**
- ✅ **Office Support**: Word (.docx), Excel (.xlsx), PowerPoint (.pptx)
- ✅ **Placeholder Extraction**: Automatic detection of {{PropertyName}} fields
- ✅ **OpenXML Manipulation**: Direct document property updates
- ✅ **Field Refresh**: Updates DOCPROPERTY fields in Word
- ✅ **Excel Integration**: Download/upload placeholders as Excel

### **🔄 Document Generation**
- ✅ **Multiple Formats**: Original, Word, HTML, EmailHtml, PDF
- ✅ **LibreOffice Conversion**: Professional format conversion
- ✅ **Auto-Download**: Single API call with `?autoDownload=true`
- ✅ **Base64 Images**: Embedded images for EmailHtml
- ✅ **Field Cleanup**: Removes LibreOffice artifacts

### **📧 Email Integration** (Primary Use Case)
- ✅ **EmailHtml Format**: Email-optimized HTML with embedded images
- ✅ **No External Images**: All images converted to base64
- ✅ **Email-Friendly CSS**: Inline styles for compatibility
- ✅ **Mobile Responsive**: Works on all email clients

### **🧩 Advanced Features**
- ✅ **Document Embedding**: Compose multiple templates into one
- ✅ **Test Mode**: Generate without saving template
- ✅ **Excel Workflow**: Download placeholders → fill → upload → generate
- ✅ **Analytics**: Track success/failure counts via CMS

### **🧹 Auto-Cleanup System**
- ✅ **15-Minute Retention**: Generated files auto-deleted
- ✅ **5-Minute Cleanup**: Runs every 5 minutes
- ✅ **Memory Efficient**: In-memory tracking with disk cleanup
- ✅ **Configurable**: Retention and cleanup intervals

---

## 📋 Prerequisites

- ✅ **.NET 9.0 SDK**
- ✅ **LibreOffice 7.0+** (for PDF/HTML conversion)
- ✅ **CMS API** running (for template data)
- ✅ **Visual Studio Code** or **Visual Studio 2022**

---

## ⚙️ Configuration

### **🔗 CMS API Connection**

```json
{
  "CmsApi": {
    "BaseUrl": "http://localhost:5000",
    "Timeout": "30"
  }
}
```

**Docker Environment**:
```bash
CMS_BASE_URL=http://cms-api:5000
CMS_API_TIMEOUT=30
```

### **🗂️ TMS Settings**

```json
{
  "TMS": {
    "DocumentRetentionHours": 0.25,      // 15 minutes
    "CleanupIntervalMinutes": 5,         // Every 5 minutes
    "MaxFileSizeMB": 100,                // 100MB limit
    "AllowedFileTypes": [".docx", ".xlsx", ".pptx"],
    "LibreOfficeTimeout": 30000,         // 30 seconds
    "SharedStoragePath": "/app/storage/TmsGenerated",
    "TempUploadPath": "/app/storage/TmsTemp"
  }
}
```

**Docker Environment**:
```bash
TMS__SharedStoragePath=/app/storage/TmsGenerated
TMS__TempUploadPath=/app/storage/TmsTemp
TMS__DocumentRetentionHours=0.25
TMS__CleanupIntervalMinutes=5
```

### **📁 Storage Directories**

```bash
# Windows
C:\ManteqStorage\
├── CmsDocuments\      # Templates (permanent, managed by CMS)
├── TmsGenerated\      # Generated docs (15min retention)
└── TmsTemp\           # Processing workspace (immediate cleanup)

# Docker
/app/storage/
├── CmsDocuments/
├── TmsGenerated/
└── TmsTemp/
```

---

## 🏃‍♂️ Quick Start

### **1. Install LibreOffice**

**Windows**:
```powershell
# Download from https://www.libreoffice.org/download/
# TMS automatically detects installation at:
# C:\Program Files\LibreOffice\program\soffice.exe
```

**Docker**: Included in Dockerfile

### **2. Start CMS First**

```bash
# TMS depends on CMS API
curl http://localhost:5000/health
# Should return healthy status
```

### **3. Update Configuration**

Edit `appsettings.json`:
```json
{
  "CmsApi": {
    "BaseUrl": "http://localhost:5000"
  },
  "TMS": {
    "SharedStoragePath": "C:\\ManteqStorage\\TmsGenerated"
  }
}
```

### **4. Run TMS**

```bash
cd TMS.WebApi
dotnet restore
dotnet run
```

**Access Points**:
- 🌐 API: `http://localhost:5267`
- 📖 Swagger: `http://localhost:5267/swagger`
- ✅ Health: `http://localhost:5267/health`

---

## 🌐 API Endpoints

### **📝 Template Management**

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/templates/register` | Register new template |
| GET | `/api/templates/{id}` | Get template metadata |
| GET | `/api/templates/{id}/properties` | Get template placeholders |
| GET | `/api/templates` | List all templates |
| DELETE | `/api/templates/{id}` | Delete template |

### **⚡ Document Generation**

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/templates/generate` | Generate document from template |
| POST | `/api/templates/generate?autoDownload=true` | Generate and download immediately |
| GET | `/api/templates/download/{generationId}` | Download generated document |
| POST | `/api/templates/generate-with-embeddings` | Generate with embedded sub-templates |

### **📊 Excel Workflow**

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/templates/{id}/download-placeholders-excel` | Download placeholders as Excel |
| POST | `/api/templates/extract-placeholders` | Extract placeholders from file |
| POST | `/api/templates/{id}/test-generate` | Generate using uploaded Excel |
| POST | `/api/templates/test-template` | Test template without saving |
| POST | `/api/templates/parse-excel` | Parse Excel to JSON |

### **📈 Analytics & Metadata**

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/templates/{id}/analytics` | Get template analytics |
| GET | `/api/templates/template-types` | Get template type enums |
| GET | `/api/templates/export-formats` | Get export format enums |

---

## 📝 API Examples

### **Template Registration**

```http
POST /api/templates/register
Content-Type: multipart/form-data

name=Customer Email Template
category=Email
description=Template for customer communications
TemplateFile=@template.docx
```

**Response:**
```json
{
  "templateId": "guid-here",
  "message": "Template registered successfully",
  "extractedPlaceholders": [
    "CustomerName",
    "PolicyNumber",
    "SupportEmail"
  ]
}
```

### **Document Generation (EmailHtml)**

```http
POST /api/templates/generate
Content-Type: application/json

{
  "templateId": "template-guid",
  "propertyValues": {
    "CustomerName": "John Smith",
    "PolicyNumber": "POL-2025-001",
    "SupportEmail": "support@example.com"
  },
  "exportFormat": "EmailHtml",
  "generatedBy": "API User"
}
```

**Response:**
```json
{
  "generationId": "gen-guid",
  "fileName": "CustomerEmail_20251118_143022_abc123.html",
  "fileSizeBytes": 15420,
  "downloadUrl": "/api/templates/download/gen-guid",
  "expiresAt": "2025-11-18T14:45:22Z",
  "exportFormat": "EmailHtml",
  "processedPlaceholders": 3
}
```

### **Auto-Download (Single Call)**

```http
POST /api/templates/generate?autoDownload=true
Content-Type: application/json

{
  "templateId": "template-guid",
  "propertyValues": { ... },
  "exportFormat": "Pdf"
}
```

**Response**: Direct file download with PDF content

### **Excel Workflow**

```http
# 1. Download placeholders as Excel
GET /api/templates/{id}/download-placeholders-excel
# Returns: Placeholders_TemplateName_20251118.xlsx

# 2. Fill Excel with values (offline)

# 3. Upload and generate
POST /api/templates/{id}/test-generate
Content-Type: multipart/form-data

ExcelFile=@filled_placeholders.xlsx
exportFormat=Word
```

---

## 🎨 Export Formats

### **📧 EmailHtml** ⭐ (Email Integration)

```json
{ "exportFormat": "EmailHtml" }
```

**Features:**
- ✅ Base64 embedded images (no external references)
- ✅ Email client compatibility (Outlook, Gmail, etc.)
- ✅ Inline CSS for styling
- ✅ LibreOffice field cleanup
- ✅ Mobile responsive

**Use Case**: Primary format for Email Service integration

### **📄 Word**

```json
{ "exportFormat": "Word" }
```

- Preserves original formatting
- Microsoft Office compatible
- Headers, footers, tables intact

### **🌐 HTML**

```json
{ "exportFormat": "Html" }
```

- Web-ready with external images
- Clean semantic markup
- CSS styling

### **📋 PDF**

```json
{ "exportFormat": "Pdf" }
```

- Professional rendering via LibreOffice
- Print-ready, searchable text
- Consistent cross-platform

### **📂 Original**

```json
{ "exportFormat": "Original" }
```

- Keeps original file type
- Placeholders filled only

---

## 🧩 Advanced Features

### **Document Embedding**

Compose multiple templates into a single document:

```http
POST /api/templates/generate-with-embeddings
Content-Type: application/json

{
  "mainTemplateId": "main-template-guid",
  "mainTemplateValues": {
    "CompanyName": "Manteq Inc",
    "Date": "2025-11-18"
  },
  "embeddings": [
    {
      "embedTemplateId": "section1-guid",
      "embedTemplateValues": {
        "Section": "Financial Summary"
      },
      "embedPlaceholder": "Section1Content"
    },
    {
      "embedTemplateId": "section2-guid",
      "embedTemplateValues": {
        "Section": "Risk Analysis"
      },
      "embedPlaceholder": "Section2Content"
    }
  ],
  "exportFormat": "Pdf",
  "generatedBy": "Report System"
}
```

### **Test Without Saving**

```http
POST /api/templates/test-template
Content-Type: multipart/form-data

TemplateFile=@test_template.docx
ExcelFile=@test_data.xlsx
exportFormat=Pdf
```

Returns generated PDF without saving template to CMS.

---

## 🐳 Docker Deployment

### **Dockerfile** (provided)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install LibreOffice for document conversion
RUN apt-get update && \
    apt-get install -y libreoffice \
                       libreoffice-writer \
                       libreoffice-calc && \
    apt-get clean

WORKDIR /app
EXPOSE 5267

# Build stages...
# Creates storage directories
RUN mkdir -p /app/storage/TmsGenerated && \
    mkdir -p /app/storage/TmsTemp

ENTRYPOINT ["dotnet", "TMS.WebApi.dll"]
```

### **Docker Compose Integration**

```yaml
services:
  tms-api:
    build:
      context: .
      dockerfile: TMS.WebApi/Dockerfile
    environment:
      - CmsApi__BaseUrl=http://cms-api:5000
      - TMS__SharedStoragePath=/app/storage/TmsGenerated
      - TMS__TempUploadPath=/app/storage/TmsTemp
      - TMS__DocumentRetentionHours=0.25
      - TMS__CleanupIntervalMinutes=5
    ports:
      - "5267:5267"
    volumes:
      - tms-storage:/app/storage
    depends_on:
      - cms-api
```

---

## 🧪 Testing

### **Health Check**

```bash
curl http://localhost:5267/health

# Response
{
  "status": "healthy",
  "service": "TMS API",
  "version": "v1",
  "timestamp": "2025-11-18T10:00:00Z"
}
```

### **Template Registration Test**

```bash
curl -X POST http://localhost:5267/api/templates/register \
  -F "name=Test Template" \
  -F "category=Testing" \
  -F "TemplateFile=@template.docx"
```

### **EmailHtml Generation Test**

```bash
curl -X POST http://localhost:5267/api/templates/generate \
  -H "Content-Type: application/json" \
  -d '{
    "templateId": "your-guid",
    "propertyValues": {
      "CustomerName": "Test User"
    },
    "exportFormat": "EmailHtml"
  }' \
  -o generated.html
```

---

## 🔧 Development

### **Project Structure**

```
TMS.WebApi/
├── Controllers/
│   ├── TemplatesController.cs         # Main API endpoints
│   └── CleanupController.cs           # Manual cleanup endpoint
├── Services/
│   ├── TemplateService.cs             # Template management
│   ├── DocumentGenerationService.cs   # Document generation
│   ├── DocumentEmbeddingService.cs    # Document composition
│   └── ExcelService.cs                # Excel operations
├── HttpClients/
│   └── CmsApiClient.cs                # CMS HTTP client
├── Models/
│   ├── TemplateModels.cs              # Request/response models
│   └── TmsSettings.cs                 # Configuration model
├── Program.cs                         # Startup configuration
└── Dockerfile                         # Docker configuration
```

### **Key Dependencies**

```xml
<PackageReference Include="DocumentFormat.OpenXml" Version="3.0.0" />
<PackageReference Include="EPPlus" Version="7.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
<PackageReference Include="Polly" Version="8.0.0" />
```

---

## 📞 Support

- **Repository**: https://github.com/SalehShalab87/Manteq-doc-system
- **Lead Developer**: Saleh Shalab
- **Email**: salehshalab2@gmail.com

---

## ✅ Production Ready

🎉 **TMS Web API is fully operational and production-ready!**

**✅ Core Features**:
- Template registration with placeholder extraction
- Multi-format document generation (EmailHtml, PDF, Word, HTML)
- LibreOffice integration for professional conversion
- Auto-cleanup with configurable retention
- Excel workflow for testing
- Document embedding for complex compositions

**✅ Microservices Integration**:
- HTTP client for CMS API communication
- Stateless architecture with Polly resilience
- Independent scaling and deployment
- Docker support with health checks

🚀 **Transforms static templates into dynamic documents with professional output quality!**