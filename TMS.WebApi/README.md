# 🎯 TMS Web API - Template Management System

A powerful Template Management System (TMS) built with ASP.NET Core 9.0 that transforms Office documents into dynamic document generation engines. **Part of the Manteq Document System** - provides advanced template processing and document generation capabilities.

## 🏗️ Role in Manteq Document System

The TMS serves as the **intelligent processing layer** that transforms static templates into dynamic documents:

```
Template Processing Pipeline:
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Upload    │───▶│  Register   │───▶│  Generate   │───▶│  Download   │
│  Template   │    │  Template   │    │  Document   │    │  Document   │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
      │                    │                  │                  │
   .docx/.xlsx          Extract           Fill Data           Auto-download
   .pptx files        Placeholders        + Convert            Available
```

## 🚀 Key Features

### **Template Management**
- ✅ **Office Document Support**: Word (.docx), Excel (.xlsx), PowerPoint (.pptx)
- ✅ **Placeholder Extraction**: Automatic detection of DOCPROPERTY fields
- ✅ **Template Registration**: Store and manage template metadata
- ✅ **Property Discovery**: Retrieve available placeholders from templates

### **Document Generation**
- ✅ **Dynamic Replacement**: Fill templates with custom data
- ✅ **Multiple Export Formats**: Word, HTML, Email HTML, PDF, Original
- ✅ **Auto-download Option**: Direct file download with `?autoDownload=true`
- ✅ **Batch Processing**: Generate multiple documents with embeddings

### **Email-Optimized HTML**
- ✅ **Base64 Embedded Images**: No external image dependencies
- ✅ **Email-Client Compatibility**: Optimized styling for email clients
- ✅ **LibreOffice Field Cleanup**: Remove unprocessed field references
- ✅ **Responsive Elements**: Mobile-friendly email layouts

### **Advanced Features**
- ✅ **Document Embedding**: Compose multiple templates into single documents
- ✅ **LibreOffice Integration**: High-quality format conversion
- ✅ **Configurable Cleanup**: Auto-remove generated files
- ✅ **Comprehensive Logging**: Detailed operation tracking

## 📋 Prerequisites

- .NET 9.0 SDK
- SQL Server Express (shared with CMS)
- **LibreOffice** (required for format conversion)
- CMS Web API (for document storage)

## ⚙️ Configuration

### **appsettings.json**
```json
{
  "TMS": {
    "DocumentRetentionHours": 0.0167,    // 1 minute (0.0167 hours)
    "CleanupIntervalMinutes": 1,         // Cleanup every minute
    "MaxFileSizeMB": 100,                // 100MB file size limit
    "AllowedFileTypes": [".docx", ".xlsx", ".pptx"],
    "LibreOfficeTimeout": 30000          // 30 seconds timeout
  }
}
```

### **Development Settings**
```json
{
  "TMS": {
    "DocumentRetentionHours": 0.0167,    // Same as production for testing
    "CleanupIntervalMinutes": 1,         // Aggressive cleanup for dev
    "MaxFileSizeMB": 50,                 // Smaller limit for development
    "AllowedFileTypes": [".docx", ".xlsx", ".pptx"],
    "LibreOfficeTimeout": 30000
  }
}
```

## 🏃‍♂️ Quick Start

### 1. **Start the TMS API**
```bash
cd TMS.WebApi
dotnet run
# Access: http://localhost:5267
# Swagger: http://localhost:5267
```

### 2. **Register a Template**
```http
POST /api/templates/register
Content-Type: multipart/form-data

{
  "name": "Invoice Template",
  "description": "Customer invoice template",
  "category": "Financial",
  "createdBy": "Admin",
  "file": [template.docx]
}
```

### 3. **Generate Document**
```http
POST /api/templates/generate?autoDownload=true
Content-Type: application/json

{
  "templateId": "your-template-id",
  "propertyValues": {
    "CustomerName": "John Doe",
    "InvoiceNumber": "INV-001",
    "Amount": "$1,500.00"
  },
  "exportFormat": "EmailHtml",
  "generatedBy": "API User"
}
```

## 📚 API Reference

### **1. Register Template**
- **POST** `/api/templates/register`
- **Purpose**: Upload and register new templates
- **Content-Type**: `multipart/form-data`
- **Response**: Template metadata with extracted placeholders

### **2. Retrieve Template**
- **GET** `/api/templates/{id}`
- **Purpose**: Get template information and properties
- **Response**: Template details and available placeholders

### **3. Get Template Properties**
- **GET** `/api/templates/{id}/properties`
- **Purpose**: List all available placeholders in template
- **Response**: Array of placeholder names and types

### **4. Generate Document**
- **POST** `/api/templates/generate`
- **Query Parameters**: 
  - `autoDownload=true` (optional) - Return file directly
- **Purpose**: Create document from template with custom data
- **Export Formats**: `Word`, `Html`, `EmailHtml`, `Pdf`, `Original`

### **5. Generate with Embeddings**
- **POST** `/api/templates/generate-with-embeddings`
- **Query Parameters**: 
  - `autoDownload=true` (optional) - Return file directly
- **Purpose**: Advanced template composition with sub-templates

### **6. Download Generated Document**
- **GET** `/api/templates/download/{generationId}`
- **Purpose**: Download previously generated documents
- **Response**: File download with appropriate content-type

## 🎯 Export Formats

### **Word (.docx)**
```json
{ "exportFormat": "Word" }
```
- ✅ Preserves original formatting
- ✅ Maintains document structure
- ✅ Compatible with Microsoft Word

### **HTML**
```json
{ "exportFormat": "Html" }
```
- ✅ Web-compatible output
- ✅ Clean HTML structure
- ✅ CSS styling preserved

### **Email HTML**
```json
{ "exportFormat": "EmailHtml" }
```
- ✅ **Base64 embedded images** (no external files)
- ✅ **Email-client optimized** styling
- ✅ **LibreOffice field cleanup**
- ✅ **Mobile-responsive** elements

### **PDF**
```json
{ "exportFormat": "Pdf" }
```
- ✅ Professional document format
- ✅ Print-ready output
- ✅ Consistent cross-platform rendering

## 🔧 Auto-Download Feature

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

## 🧪 Testing Examples

### **Test Email HTML Generation**
```bash
curl -X POST "http://localhost:5267/api/templates/generate?autoDownload=true" \
  -H "Content-Type: application/json" \
  -d '{
    "templateId": "your-template-id",
    "propertyValues": {
      "CustomerName": "John Doe",
      "PolicyNumber": "POL-123456",
      "PolicyStartDate": "2025-09-10",
      "PolicyEndDate": "2026-09-09"
    },
    "exportFormat": "EmailHtml",
    "generatedBy": "Test User"
  }' \
  --output "test-email.html"
```

### **Verify Image Embedding**
```bash
# Check if images are embedded as base64
grep -o "data:image/[^;]*;base64,[^\"]*" test-email.html
```

## 🚀 Integration Examples

### **Frontend Integration**
```javascript
// Generate and auto-download
const response = await fetch('/api/templates/generate?autoDownload=true', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    templateId: 'template-id',
    propertyValues: { CustomerName: 'John Doe' },
    exportFormat: 'EmailHtml'
  })
});

// Handle file download
const blob = await response.blob();
const url = window.URL.createObjectURL(blob);
const a = document.createElement('a');
a.href = url;
a.download = 'generated-document.html';
a.click();
```

### **Email Service Integration**
```csharp
// Generate email-friendly HTML
var request = new DocumentGenerationRequest
{
    TemplateId = templateId,
    PropertyValues = customerData,
    ExportFormat = ExportFormat.EmailHtml
};

var response = await _tmsService.GenerateDocumentAsync(request);
var htmlContent = await _tmsService.DownloadGeneratedDocumentAsync(response.GenerationId);

// Send email with embedded images
await _emailService.SendHtmlEmailAsync(recipient, subject, htmlContent);
```

## 📊 Performance & Cleanup

### **Automatic Cleanup**
- ⏰ **Cleanup Interval**: Every 1 minute (configurable)
- 🗑️ **Retention**: 1 minute (0.0167 hours)
- 🧹 **Auto-removal**: Generated files deleted automatically
- 💾 **Memory Efficient**: Temporary file tracking

### **LibreOffice Integration**
- ⚡ **Timeout**: 30 seconds (configurable)
- 🔄 **Process Management**: Automatic cleanup of hanging processes
- 📈 **High Quality**: Professional format conversion
- 🖼️ **Image Handling**: Automatic base64 embedding for email

## 🔒 Security Features

### **Controller Exclusion**
```csharp
// TMS only exposes Template endpoints, not CMS endpoints
public class ControllerExclusionConvention : IControllerModelConvention
{
    // Automatically hides CMS controllers from TMS API
}
```

### **File Validation**
- 📁 **Allowed Types**: Only .docx, .xlsx, .pptx files
- 📏 **Size Limits**: Configurable file size restrictions
- 🛡️ **Input Validation**: Comprehensive request validation

## 🎯 Use Cases

### **Business Documents**
- 📄 **Invoices**: Generate customer invoices from templates
- 📋 **Contracts**: Create contracts with dynamic terms
- 📊 **Reports**: Generate data-driven reports
- 📧 **Email Templates**: Create email-optimized HTML

### **Integration Scenarios**
- 🔄 **Workflow Automation**: Automated document generation
- 📧 **Email Marketing**: Dynamic email content generation
- 📱 **Mobile Apps**: Server-side document processing
- 🌐 **Web Applications**: On-demand document creation

## 🛠️ Development Notes

### **Architecture Decisions**
- **Separation of Concerns**: TMS processes, CMS stores
- **Clean APIs**: Only TMS endpoints exposed to clients
- **Configuration-Driven**: All settings externalized
- **Async Processing**: Non-blocking operations throughout

### **LibreOffice Dependency**
```bash
# Install LibreOffice (Windows)
# Download from: https://www.libreoffice.org/download/download/
# Common locations checked:
# - C:\Program Files\LibreOffice\program\soffice.exe
# - C:\Program Files (x86)\LibreOffice\program\soffice.exe
```

## 📝 Troubleshooting

### **LibreOffice Issues**
```bash
# Check if LibreOffice is installed and accessible
soffice --version

# Test conversion manually
soffice --headless --convert-to pdf document.docx
```

### **Database Connectivity**
```bash
# Verify connection string in appsettings.json
"DefaultConnection": "Data Source=SALEH-PC\\SQLEXPRESS;Initial Catalog=CmsDatabase_Dev;..."
```

### **File Permissions**
```bash
# Ensure TMS can write to GeneratedDocuments directory
# Default: TMS.WebApi/GeneratedDocuments/
```

## 🔗 Related Systems

- **CMS Web API**: Document storage backend ([CMS README](../CMS.Webapi/README.md))
- **Main System**: Complete documentation ([Main README](../README.md))

---

## 📞 Support

- **Author**: Saleh Manteq  
- **Email**: saleh@manteq.com
- **Repository**: https://github.com/SalehShalab87/Manteq-doc-system

**Status**: ✅ **Production Ready** - TMS provides comprehensive template management and document generation capabilities with professional-grade output quality.

The Template Management System transforms static Office documents into powerful, dynamic document generation engines with multiple export formats, auto-download capabilities, and seamless email integration.
