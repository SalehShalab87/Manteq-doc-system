# 📄 Manteq Document System

A comprehensive document automation platform consisting of **Content Management System (CMS)** and **Template Management System (TMS)** built with ASP.NET Core 9.0.

## 🏗️ System Architecture

```
Manteq Document System
├── 📁 CMS.WebApi/              # Content Management System
│   ├── Document storage & retrieval
│   ├── File management services
│   └── Database integration
└── 📁 TMS.WebApi/              # Template Management System
    ├── Template processing
    ├── Document generation
    ├── Multiple export formats
    └── Auto-download functionality
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

## 📋 Prerequisites

- .NET 9.0 SDK
- SQL Server Express (SQLEXPRESS instance)
- LibreOffice (for TMS document conversion)
- Visual Studio Code or Visual Studio

## ⚙️ Quick Start

### 1. **Database Setup**
```sql
-- Create database (or configure connection string)
CREATE DATABASE ManteqCmsDb;
```

### 2. **Build Options**
```bash
# Option 1: Build entire system
dotnet build ManteqDocumentSystem.sln

# Option 2: Build TMS with dependencies
cd TMS.WebApi
dotnet build TMS.WebApi.sln

# Option 3: Build individual projects
cd CMS.WebApi && dotnet build
cd TMS.WebApi && dotnet build
```

### 3. **Run CMS API**
```bash
cd CMS.WebApi
dotnet run
# Access: http://localhost:5077 (Swagger: https://localhost:7276)
```

### 4. **Run TMS API**
```bash
cd TMS.WebApi
dotnet run
# Access: http://localhost:5267 (Swagger: http://localhost:5267)
```

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
├── ManteqDocumentSystem.sln    # Main solution (CMS + TMS)
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
├── .gitignore                  # Ignores generated files
└── README.md                   # This file
```

### **Solution Files**
- **`ManteqDocumentSystem.sln`** - Complete system build (recommended for development)
- **`TMS.WebApi/TMS.WebApi.sln`** - TMS-focused build with CMS dependency

## 🧪 Testing

### **Swagger UI Access**
- **CMS**: https://localhost:7276
- **TMS**: http://localhost:5267

### **Sample Workflow**
1. **Upload template** via CMS
2. **Register template** in TMS
3. **Generate document** with custom data
4. **Auto-download** or manual download

## 🚀 Deployment

### **Development**
```bash
# Start both systems
dotnet run --project CMS.WebApi
dotnet run --project TMS.WebApi
```

### **Production**
```bash
# Build for production
dotnet publish CMS.WebApi -c Release
dotnet publish TMS.WebApi -c Release
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

## 📝 Development Notes

- **Architecture**: Clean separation between CMS (storage) and TMS (processing)
- **Security**: Controller exclusion prevents exposing internal CMS endpoints
- **Scalability**: Configurable cleanup and retention policies
- **Maintainability**: Comprehensive logging and error handling

---

## 📞 Support

- **Author**: Saleh Shalab
- **Email**: salehshalab2@gmail.com
- **Repository**: https://github.com/SalehShalab87/Manteq-doc-system

**Status**: ✅ **Production Ready** - Both CMS and TMS are fully functional and tested.

The system provides a complete document automation solution with professional-grade template processing, multiple export formats, and seamless integration capabilities.