# 🎯 TMS Web API - Template Management System

**Template processing engine** that transforms Office d### **🔒 Environment Variables (Required)**
Create `.env` file in the TMS.WebApi directory:

**File: `TMS.WebApi/.env`**
```env
DB_SERVER=YOUR_SERVER\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev  
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true
```

### **📁 Storage Configuration**
```json
// appsettings.json (no database credentials here!)
{
  "FileStorage": {
    "Path": "C:\\ManteqStorage_Shared\\CmsDocuments"  // For internal CMS services
  },
  "TMS": {
    "DocumentRetentionHours": 0.25,        // 15 minutes retentionmic document generators. Built with ASP.NET Core 9.0 and LibreOffice integration.

> 🎯 **Role**: Intelligent document processing layer that converts static templates into data-driven documents with multiple export formats.

## 🏗️ Architecture Integration

The TMS serves as the **document processing hub** in the Manteq ecosystem:

```
┌─────────────────────────────────────────────────────────┐
│                MANTEQ DOCUMENT SYSTEM                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📧 Email Service ────── 🎯 TMS API ────── 📁 CMS API   │
│  (Port 5030)           (Port 5267)      (Port 5000)   │
│                                                         │
│  Calls TMS to          Template Engine   Stores ALL    │
│  generate EmailHtml    + LibreOffice     documents     │
│  content for emails    + Placeholders    permanently   │
│                                                         │
└─────────────────────────────────────────────────────────┘
                            │
                ┌───────────┴────────────┐
                │    PROCESSING FLOW     │
                │                       │
                │ 1. Register Template  │ → CMS stores file
                │ 2. Extract Properties │ → Find {{placeholders}}
                │ 3. Generate Document  │ → Fill data + convert
                │ 4. Auto-Cleanup      │ → Remove after 15min
                └───────────────────────┘
```

### **🔄 Service Integration**
- **TMS → CMS**: Stores templates as documents (permanent)
- **TMS → LibreOffice**: Converts documents to HTML/PDF formats  
- **Email → TMS**: Requests EmailHtml generation for email content
- **TMS Storage**: Generated documents auto-cleaned (temporary)

## 🚀 Key Features

### **📄 Template Management**
- ✅ **Office Document Support**: Word (.docx), Excel (.xlsx), PowerPoint (.pptx)
- ✅ **CMS Integration**: Templates stored permanently in shared CMS storage
- ✅ **Placeholder Discovery**: Automatic extraction of {{PropertyName}} fields
- ✅ **Template Metadata**: Name, description, and property tracking
- ✅ **Property API**: Get available placeholders for dynamic form generation

### **⚡ Document Generation**
- ✅ **Dynamic Data Replacement**: Fill templates with custom property values
- ✅ **Multiple Export Formats**: Word, HTML, EmailHtml, PDF, Original format
- ✅ **LibreOffice Integration**: Professional-quality format conversion
- ✅ **Auto-Download**: Direct file download with `?autoDownload=true` parameter
- ✅ **Email-Optimized HTML**: Base64 embedded images for email compatibility

### **🧹 Smart File Management**
- ✅ **Auto-Cleanup**: Generated files removed every 5 minutes  
- ✅ **Retention Policy**: 15-minute retention for downloaded files
- ✅ **Shared Storage**: Production-grade file storage architecture
- ✅ **Memory Efficiency**: In-memory tracking with disk cleanup

### **📧 Email Service Features**
- ✅ **EmailHtml Format**: Email-client compatible HTML with embedded images
- ✅ **Field Cleanup**: LibreOffice field references automatically removed
- ✅ **Responsive Design**: Mobile-friendly email layouts
- ✅ **Base64 Images**: No external image dependencies for email clients

### **🔧 Advanced Capabilities**
- ✅ **Document Embedding**: Compose multiple templates into single documents
- ✅ **Error Handling**: Comprehensive error responses and logging
- ✅ **Performance Monitoring**: Detailed operation tracking and metrics
- ✅ **Configurable Settings**: Customizable retention, cleanup, and file limits

## 📋 Prerequisites

- ✅ **.NET 9.0 SDK**
- ✅ **SQL Server Express** (shared `CmsDatabase_Dev` with CMS)
- ✅ **LibreOffice** - Download from https://www.libreoffice.org/download/
- ✅ **CMS Web API** - Must be built and available for dependency injection
- ✅ **Shared Storage** - `C:\ManteqStorage_Shared\` directories setup

### **🔧 LibreOffice Installation**
TMS requires LibreOffice for document conversion:
```powershell
# Download and install LibreOffice
# TMS automatically detects installation at:
# - C:\Program Files\LibreOffice\program\soffice.exe
# - C:\Program Files (x86)\LibreOffice\program\soffice.exe

# Verify installation
Test-Path "C:\Program Files\LibreOffice\program\soffice.exe"
```

### **📁 Storage Architecture**
```
C:\ManteqStorage_Shared\
├── CmsDocuments\      # Templates stored here (via CMS) - PERMANENT
├── TmsGenerated\      # Generated documents - AUTO-CLEANED (15min)  
└── TmsTemp\          # Processing workspace - IMMEDIATE CLEANUP
```

## ⚙️ Configuration


### **📁 Storage Configuration**
```json
{
  "FileStorage": {
    "Path": "C:\\ManteqStorage_Shared\\CmsDocuments"  // For internal CMS services
  },
  "TMS": {
    "DocumentRetentionHours": 0.25,        // 15 minutes retention
    "CleanupIntervalMinutes": 5,           // Cleanup every 5 minutes
    "MaxFileSizeMB": 100,                  // 100MB file limit
    "AllowedFileTypes": [".docx", ".xlsx", ".pptx"],
    "LibreOfficeTimeout": 30000,           // 30 seconds
    "SharedStoragePath": "C:\\ManteqStorage_Shared\\TmsGenerated",  // Generated docs
    "TempUploadPath": "C:\\ManteqStorage_Shared\\TmsTemp"           // Processing temp
  }
}
```

### **🧹 Cleanup Behavior**
- **Templates**: ❌ **NEVER cleaned** - stored permanently in CMS storage
- **Generated Documents**: ✅ **Auto-cleanup** every 5 minutes - removed after 15 minutes  
- **Temp Files**: ✅ **Immediate cleanup** after processing

### **⚖️ Development vs Production**
```json
// Development (appsettings.Development.json)
{
  "TMS": {
    "DocumentRetentionHours": 0.25,      // Same retention
    "CleanupIntervalMinutes": 5,         // Same cleanup
    "MaxFileSizeMB": 50,                 // Smaller limit for dev
    "LibreOfficeTimeout": 30000
  }
}
```

## 🏃‍♂️ Quick Start

### **1. Setup Prerequisites**
```powershell
# Ensure CMS is built (TMS depends on CMS)
cd ..\CMS.WebApi
dotnet build

# Ensure shared storage exists
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\CmsDocuments" -Force
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\TmsGenerated" -Force
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\TmsTemp" -Force

# Verify LibreOffice installation
Test-Path "C:\Program Files\LibreOffice\program\soffice.exe"
```

### **2. Build and Run TMS**
```powershell
# Option 1: Run TMS with CMS dependency
cd TMS.WebApi
dotnet build TMS.WebApi.sln  # Includes CMS project
dotnet run

# Option 2: Build entire Manteq system
cd ..  # From repo root
dotnet build ManteqDocumentSystem.sln
cd TMS.WebApi
dotnet run

# 🌐 Access TMS API: http://localhost:5267
# 📖 Swagger UI: http://localhost:5267/swagger
```

### **3. Test TMS Service**
```powershell
# Health check
curl http://localhost:5267/api/templates/health
# Expected: {"status": "healthy", "service": "TMS"}

# Verify CMS integration
curl http://localhost:5000/api/documents/health
# CMS should also be accessible (TMS uses it internally)
```

### **4. Register Your First Template**
```powershell
# Upload a template file (creates document in CMS internally)
curl -X POST "http://localhost:5267/api/templates/register" `
     -F "file=@Email_Template.docx" `
     -F "name=Customer Email Template" `
     -F "description=Template for customer communications"

# Response includes templateId for document generation
```

### **5. Generate Document from Template**
```powershell
# Generate document with data
curl -X POST "http://localhost:5267/api/templates/generate" `
     -H "Content-Type: application/json" `
     -d '{
       "templateId": "your-template-id",
       "propertyValues": {
         "CustomerName": "John Smith",
         "PolicyNumber": "POL-2025-001"
       },
       "exportFormat": "EmailHtml",
       "generatedBy": "Quick Start Test"
     }'

# Response includes downloadUrl for the generated file
```

> 🎉 **Success!** TMS is running and processing templates with CMS integration.
```

## 🌐 API Reference

### **📝 Template Registration**
```http
POST http://localhost:5267/api/templates/register
Content-Type: multipart/form-data

Parameters:
- file: Template file (.docx/.xlsx/.pptx) - Required
- name: Template name - Required  
- description: Template description - Optional
```

**Response:**
```json
{
  "templateId": "96cec0ae-a1f9-4e01-8e07-16ddd57b4b25",
  "name": "Customer Email Template",
  "description": "Template for customer communications",
  "message": "Template registered successfully",
  "cmsDocumentId": "1d4c5082-021d-42a4-9f39-794671cf8bac",
  "extractedProperties": ["CustomerName", "PolicyNumber", "SupportEmail"]
}
```

### **📄 Template Information**
```http
GET http://localhost:5267/api/templates/{templateId}
GET http://localhost:5267/api/templates/{templateId}/properties
```

**Properties Response:**
```json
{
  "templateId": "96cec0ae-a1f9-4e01-8e07-16ddd57b4b25",
  "properties": [
    {
      "name": "CustomerName",
      "type": "Text",
      "required": true
    },
    {
      "name": "PolicyNumber", 
      "type": "Text",
      "required": true
    }
  ]
}
```

### **⚡ Document Generation**
```http
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
```

**Generation Response:**
```json
{
  "generationId": "abc123-def456-ghi789",
  "message": "Document generated successfully", 
  "fileName": "CustomerEmail_20250911_135230.html",
  "fileSizeBytes": 15420,
  "downloadUrl": "/api/templates/download/abc123-def456-ghi789",
  "expiresAt": "2025-09-11T14:07:30Z",
  "exportFormat": "EmailHtml",
  "processedPlaceholders": 3
}
```

### **📥 Document Download**
```http
GET http://localhost:5267/api/templates/download/{generationId}
```
Returns the generated file with appropriate headers.

### **🔍 Health Check**
```http
GET http://localhost:5267/api/templates/health
```

**Response:**
```json
{
  "status": "healthy",
  "service": "TMS", 
  "timestamp": "2025-09-11T13:45:00Z",
  "database": "connected",
  "libreOffice": "available",
  "cmsIntegration": "active"
}
```

## � Export Formats

### **📄 Word (.docx)**
```json
{ "exportFormat": "Word" }
```
- ✅ **Preserves formatting**: Original styling and layout maintained
- ✅ **Document structure**: Headers, footers, tables intact  
- ✅ **Microsoft compatibility**: Opens perfectly in Word
- ✅ **Use case**: Official documents, contracts, reports

### **🌐 HTML**
```json
{ "exportFormat": "Html" }
```
- ✅ **Web-ready**: Direct embedding in websites
- ✅ **Clean markup**: Semantic HTML structure
- ✅ **CSS styling**: Responsive design elements
- ✅ **Use case**: Web content, documentation sites

### **📧 EmailHtml** ⭐ **Email Service Integration**
```json  
{ "exportFormat": "EmailHtml" }
```
- ✅ **Base64 Images**: All images embedded (no external refs)
- ✅ **Email clients**: Optimized for Outlook, Gmail, etc.
- ✅ **Field cleanup**: LibreOffice artifacts removed
- ✅ **Mobile responsive**: Works on all devices  
- ✅ **Use case**: **Email content generation for Email Service**

### **📋 PDF**
```json
{ "exportFormat": "Pdf" }
```
- ✅ **Professional**: High-quality document rendering
- ✅ **Print-ready**: Consistent formatting across platforms
- ✅ **Searchable**: Text-based PDF with proper fonts
- ✅ **Use case**: Invoices, certificates, legal documents

### **📂 Original**  
```json
{ "exportFormat": "Original" }
```
- ✅ **Unchanged format**: Keeps original file type
- ✅ **Data replacement**: Placeholders filled, format preserved
- ✅ **Use case**: When specific Office format required

## ⚡ Auto-Download Feature

Add `?autoDownload=true` to generation endpoints for immediate file return:

```http
POST http://localhost:5267/api/templates/generate?autoDownload=true
# Returns file directly instead of generation metadata
```

**Benefits:**
- 🚀 **Instant download**: No separate download call needed
- 📱 **Mobile-friendly**: Direct file response  
- 🔄 **Streamlined workflow**: Single API call for generation + download

### **Traditional Flow** (2 API calls)
```bash
# 1. Generate document
POST /api/templates/generate
# Response: { "generationId": "abc-123", "downloadUrl": "..." }

# 2. Download document
GET /api/templates/download/abc-123
# Response: File content
```

### **Auto-Download Flow** (1 API call)
```bash
# Generate and download in one step
POST /api/templates/generate?autoDownload=true
# Response: Direct file download
```

### **Benefits**
- ⚡ **Fewer API calls** - Single request for generation + download
- 🔄 **Immediate response** - No need to poll for completion
- 📧 **Email integration** - Perfect for automated email systems
- 🚀 **Performance** - Reduced latency for client applications

## 🗂️ Project Structure

```
TMS.WebApi/
├── Controllers/
│   └── TemplatesController.cs          # Main API endpoints
├── Services/
│   ├── ITemplateService.cs            # Template management interface
│   ├── TemplateService.cs             # Template management implementation
│   ├── IDocumentGenerationService.cs  # Document generation interface
│   ├── DocumentGenerationService.cs   # Document generation implementation
│   ├── IDocumentEmbeddingService.cs   # Document embedding interface
│   └── DocumentEmbeddingService.cs    # Document embedding implementation
├── Models/
│   ├── TemplateModels.cs              # Request/Response models
│   ├── TmsSettings.cs                 # Configuration model
│   └── GeneratedDocument.cs          # Generated document tracking
├── Infrastructure/
│   └── ControllerExclusionConvention.cs # Security - Hide CMS controllers
├── GeneratedDocuments/                 # Auto-cleanup directory
├── appsettings.json                   # Production configuration
├── appsettings.Development.json       # Development configuration
└── README.md                          # This file
```

## 🔍 Template Processing

### **Placeholder Detection**
TMS automatically detects placeholders in Office documents:

```docx
Template Content:
"Dear {DOCPROPERTY CustomerName}, your policy {DOCPROPERTY PolicyNumber} is ready."

Extracted Placeholders:
- CustomerName
- PolicyNumber
```

### **Supported Placeholder Formats**
- **DOCPROPERTY fields**: `{DOCPROPERTY PropertyName}`
- **Custom properties**: Document metadata properties
- **Header/Footer placeholders**: Full document processing

## 🧪 Testing the TMS

### **🔍 Health Check Test**
```powershell
# Verify TMS is running and LibreOffice is available
curl http://localhost:5267/api/templates/health

# Expected response:
# {"status": "healthy", "service": "TMS", "libreOffice": "available"}
```

### **📝 Template Registration Test**
```powershell
# Upload template (creates document in CMS internally)
curl -X POST "http://localhost:5267/api/templates/register" `
     -F "file=@Email_Template.docx" `
     -F "name=Test Template" `
     -F "description=Testing template registration"

# Check template properties
curl http://localhost:5267/api/templates/{templateId}/properties
```

### **⚡ Document Generation Test**
```powershell
# Generate EmailHtml for email integration
curl -X POST "http://localhost:5267/api/templates/generate" `
     -H "Content-Type: application/json" `
     -d '{
       "templateId": "your-template-id",
       "propertyValues": {
         "CustomerName": "Test Customer",
         "PolicyNumber": "TEST-001"
       },
       "exportFormat": "EmailHtml",
       "generatedBy": "Test Suite"
     }'

# Verify base64 image embedding (for EmailHtml)
# Generated HTML should contain: data:image/...;base64,...
```

### **📥 Auto-Download Test**
```powershell
# Generate and download immediately
curl -X POST "http://localhost:5267/api/templates/generate?autoDownload=true" `
     -H "Content-Type: application/json" `
     -d '{...}' `
     --output generated-document.html
```

### **🧹 Cleanup Verification**
```powershell
# Check generated files (should be cleaned up after 15 minutes)
Get-ChildItem "C:\ManteqStorage_Shared\TmsGenerated\"

# Check permanent templates (should remain)
Get-ChildItem "C:\ManteqStorage_Shared\CmsDocuments\"
```

## � Integration Examples

### **📧 Email Service Integration**
The Email Service calls TMS to generate content:
```http
# Email Service calls TMS internally like this:
POST http://localhost:5267/api/templates/generate
{
  "templateId": "customer-email-template",
  "propertyValues": { "CustomerName": "John Smith" },
  "exportFormat": "EmailHtml"  // Email-optimized with base64 images
}

# TMS returns HTML content that becomes the email body
```

### **🔧 .NET Service Integration**
```csharp
// Use TMS services directly in other .NET projects
public class DocumentAutomationService
{
    private readonly IDocumentGenerationService _tmsGeneration;
    
    public DocumentAutomationService(IDocumentGenerationService tmsGeneration)
    {
        _tmsGeneration = tmsGeneration;
    }
    
    public async Task<string> GenerateEmailContentAsync(Guid templateId, Dictionary<string, string> data)
    {
        var request = new DocumentGenerationRequest
        {
            TemplateId = templateId,
            PropertyValues = data,
            ExportFormat = ExportFormat.EmailHtml,
            GeneratedBy = "Service Integration"
        };
        
        var response = await _tmsGeneration.GenerateDocumentAsync(request);
        // Returns HTML content ready for email
        return response.Content;
    }
}
```


## 📊 Performance & Cleanup

### **Automatic Cleanup**
## 📊 Performance & Monitoring

### **🧹 Automatic Cleanup System**
- ⏰ **Cleanup Interval**: Every 5 minutes (configurable)
- 🗑️ **Retention Policy**: 15 minutes (0.25 hours)  
- 🧹 **Auto-removal**: Generated files deleted automatically
- 💾 **Memory Efficient**: In-memory tracking with disk cleanup
- 📊 **Storage Monitoring**: Track `TmsGenerated` directory size

### **⚡ LibreOffice Performance**
- ⚡ **Timeout**: 30 seconds (configurable)
- 🔄 **Process Management**: Automatic cleanup of hanging processes
- 📈 **High Quality**: Professional format conversion
- 🖼️ **Image Processing**: Automatic base64 embedding for EmailHtml
- 🛠️ **Error Handling**: Graceful fallback when LibreOffice unavailable

### **📈 Monitoring Metrics**
```
📊 Key Metrics to Watch:
├── Generated documents per minute
├── Average generation time  
├── LibreOffice conversion success rate
├── Cleanup efficiency (files removed vs created)
├── Storage usage in TmsGenerated directory
└── Template registration frequency
```

## 🔒 Security & Architecture

### **🛡️ Controller Exclusion Pattern**
```csharp
// TMS automatically hides CMS endpoints from public API
public class ControllerExclusionConvention : IControllerModelConvention
{
    // Only TMS endpoints exposed, CMS used internally only
}
```

### **📁 File Validation & Security**
- � **Allowed Types**: Only `.docx`, `.xlsx`, `.pptx` files accepted
- 📏 **Size Limits**: Configurable max file size (100MB default)
- 🛡️ **Input Validation**: Comprehensive request validation and sanitization
- 🔐 **Path Security**: Safe file naming with GUID-based storage

### **🏗️ Architecture Principles**
- **🎯 Single Responsibility**: TMS processes templates, CMS stores documents
- **🔄 Loose Coupling**: Services communicate via well-defined interfaces  
- **📊 Configuration-Driven**: All settings externalized to appsettings.json
- **⚡ Async-First**: Non-blocking operations throughout the pipeline

## 🎯 Common Use Cases

### **📧 Email Automation (Primary Integration)**
```
Email Service → TMS → Generate EmailHtml → Send as email body
• Customer communications
• Invoice notifications  
• Policy documents
• Marketing templates
```

### **📄 Business Document Generation**
- **📊 Invoices**: Dynamic customer billing documents
- **📋 Contracts**: Automated contract generation with custom terms
- **📈 Reports**: Data-driven business reports
- **📑 Certificates**: Personalized certificates and credentials

### **🌐 Web Application Integration**
- **📱 Mobile Apps**: Server-side document processing
- **🖥️ Web Dashboards**: On-demand document creation
- **🔄 Workflow Systems**: Automated document steps
- **📊 Data Export**: Convert data to formatted documents

### **⚙️ Enterprise Automation**
- **🔄 Batch Processing**: Generate multiple documents from templates
- **📅 Scheduled Generation**: Time-based document creation
- **🔗 API Integration**: RESTful document services
- **📦 Document Packaging**: Combine multiple templates

## 🛠️ Development & Deployment

### **🏗️ Development Setup**
```powershell
# Prerequisites: CMS must be built first (dependency)
cd ..\CMS.WebApi
dotnet build

# Build TMS with CMS dependency
cd ..\TMS.WebApi
dotnet build TMS.WebApi.sln

# Verify LibreOffice installation
Test-Path "C:\Program Files\LibreOffice\program\soffice.exe"

# Run TMS
dotnet run
```

### **🚀 Production Deployment**
```powershell
# Build for production
dotnet publish TMS.WebApi.sln -c Release -o ./publish

# Production checklist:
# ✅ LibreOffice installed on production server
# ✅ Shared storage accessible to all services  
# ✅ Database connection string configured
# ✅ File size and retention policies set appropriately
```

### **🔧 Configuration for Production**
```json
{
  "TMS": {
    "DocumentRetentionHours": 1.0,         // Longer retention in prod
    "CleanupIntervalMinutes": 15,          // Less frequent cleanup
    "MaxFileSizeMB": 200,                  // Larger files allowed
    "LibreOfficeTimeout": 60000            // Longer timeout for complex docs
  }
}
```

## ❌ Troubleshooting

### **🔍 LibreOffice Issues**
```powershell
# Check LibreOffice installation
Test-Path "C:\Program Files\LibreOffice\program\soffice.exe"
Test-Path "C:\Program Files (x86)\LibreOffice\program\soffice.exe"

# Test manual conversion
& "C:\Program Files\LibreOffice\program\soffice.exe" --headless --convert-to pdf --outdir . document.docx

# If LibreOffice hangs, kill processes
Get-Process soffice* | Stop-Process -Force
```

### **🗄️ Database Connection Problems**
```sql
-- Verify shared database exists
USE master
SELECT name FROM sys.databases WHERE name = 'CmsDatabase_Dev'

-- Check Tables table exists (TMS creates it)
USE CmsDatabase_Dev
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Templates'
```

### **📁 Storage Issues**
```powershell
# Verify storage directories exist and are accessible
Test-Path "C:\ManteqStorage_Shared\CmsDocuments"      # Templates (permanent)
Test-Path "C:\ManteqStorage_Shared\TmsGenerated"      # Generated docs (temp)
Test-Path "C:\ManteqStorage_Shared\TmsTemp"           # Processing workspace

# Check permissions
Get-Acl "C:\ManteqStorage_Shared" | Format-List
```

### **🔗 CMS Integration Issues**
```powershell
# Verify CMS is built and available
Test-Path "..\CMS.WebApi\bin\Debug\net9.0\CMS.WebApi.dll"

# Test CMS service directly
curl http://localhost:5000/api/documents/health

# Check TMS can access CMS services
curl http://localhost:5267/api/templates/health
```

### **🔄 Common Error Messages**
- **"LibreOffice timeout"** → LibreOffice not responding, increase timeout or restart process
- **"Template not found"** → Template ID invalid or cleanup removed generated file  
- **"CMS integration failed"** → CMS service not available or database connection issue
- **"Generated document expired"** → File removed by cleanup, check retention settings

## 📚 Additional Resources

### **🔗 Related Documentation**
- **[Main System README](../README.md)** - Complete Manteq Document System overview
- **[CMS README](../CMS.WebApi/README.md)** - Document storage service (TMS dependency)
- **[Email Service README](../EmailService.WebApi/README.md)** - Email automation (uses TMS)
- **[Team Developer Guide](../TEAM_GUIDE.md)** - Comprehensive development guide

### **🌐 API Documentation**
- **Swagger UI**: http://localhost:5267/swagger (when running)
- **OpenAPI Spec**: Available at runtime for integration tools

### **🧪 Testing Tools**
- **Postman**: Import OpenAPI spec for complete API testing
- **curl**: Command-line testing examples shown above
- **LibreOffice**: Manual document conversion testing

---

## 📞 Support and Contact

- **👨‍💻 Lead Developer**: Saleh Shalab
- **📧 Email**: salehshalab2@gmail.com
- **🌐 Repository**: https://github.com/SalehShalab87/Manteq-doc-system
- **🐛 Issues**: Use GitHub Issues for bug reports and feature requests

---

## ✅ Status: Production Ready

🎉 **TMS Web API is fully operational and production-ready!**

**✅ Core Features Complete:**
- Template registration with CMS integration
- Multi-format document generation (Word, HTML, EmailHtml, PDF)
- LibreOffice integration for professional conversion
- Auto-cleanup system with configurable retention
- Email Service integration for HTML content generation

**✅ Integration Status:**
- ✅ CMS Integration: Fully implemented and tested
- ✅ Email Service Ready: EmailHtml generation with base64 images
- ✅ LibreOffice: High-quality document conversion
- ✅ Database Schema: Stable with CMS foreign key relationships
- ✅ Storage Architecture: Production-grade shared storage

**🚀 The TMS transforms static Office documents into powerful, dynamic document generation engines with professional-grade output quality and seamless microservice integration.**
