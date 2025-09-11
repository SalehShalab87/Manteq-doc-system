# 📁 CMS Web API - Content Management System

**Content Management Syste## ⚙️ Configuration

### **🔒 Environment Variables (Required)**
Create `.env` file in the CMS.WebApi directory:

**File: `CMS.WebApi/.env`**
```env
DB_SERVER=YOUR_SERVER\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev  
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true
```

### **🗂️ Production Storage Configuration**
```json
// appsettings.json (no database credentials here!)
{
  "FileStorage": {
    "Path": "C:\\ManteqStorage_Shared\\CmsDocuments"  // Shared with TMS
  }
}
```core document storage and retrieval services for the Manteq Document System. Built with ASP.NET Core 9.0 and Entity Framework Core.

> 🎯 **Role**: Foundation storage layer that manages all documents and templates for TMS and Email Service integration.

## 🏗️ Architecture Integration

The CMS serves as the **central document repository** for the complete Manteq platform:

```
┌─────────────────────────────────────────────────────────┐
│                MANTEQ DOCUMENT SYSTEM                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📧 Email Service ────── 🎯 TMS API ────── 📁 CMS API   │
│  (Port 5030)           (Port 5267)      (Port 5000)   │
│                                                         │
│  Uses generated        Processes         Stores ALL    │
│  content from TMS      templates →       documents     │
│                       calls CMS internally              │
└─────────────────────────────────────────────────────────┘
                            │
                 ┌──────────┴───────────┐
                 │   SHARED STORAGE     │
                 │ C:\ManteqStorage_    │
                 │      Shared\         │
                 │                     │
                 │ • CmsDocuments/     │ ← All files here
                 │ • Templates         │
                 │ • Generated docs    │
                 └─────────────────────┘
```

### **🔄 Service Relationships**
- **CMS ← TMS**: TMS creates documents in CMS when templates are registered
- **CMS ← Email**: Email service can attach CMS documents to emails  
- **Database**: Single shared `CmsDatabase_Dev` for all services
- **Storage**: Centralized file storage shared by all services

## 🚀 Features

### **📁 Core Document Management**
- ✅ **Document Registration**: Upload and store documents with metadata
- ✅ **Document Retrieval**: Get document metadata and download URLs  
- ✅ **File Storage**: Production-grade shared storage architecture
- ✅ **Database Integration**: Entity Framework with SQL Server
- ✅ **RESTful API**: Clean REST endpoints with Swagger documentation

### **🔒 Security & Validation**
- ✅ **File Size Limits**: 50MB maximum with configurable validation
- ✅ **File Type Support**: Documents (.docx, .xlsx, .pptx), images, archives
- ✅ **Input Validation**: Comprehensive request validation and error handling
- ✅ **Safe File Naming**: Automatic sanitization and GUID-based naming

### **🎯 Integration Features**  
- ✅ **TMS Integration**: Internal services used by Template Management System
- ✅ **Email Integration**: Document attachment support for Email Service
- ✅ **Microservice Architecture**: Clean separation of concerns
- ✅ **Shared Database**: Single database shared across all Manteq services

### **⚡ Production Ready**
- ✅ **Error Handling**: Comprehensive error responses and logging
- ✅ **Performance**: Optimized file I/O and database queries
- ✅ **Monitoring**: Health checks and service status endpoints
- ✅ **Scalability**: Stateless design ready for horizontal scaling

## 📋 Prerequisites

- ✅ **.NET 9.0 SDK**
- ✅ **SQL Server Express** (SQLEXPRESS instance)
- ✅ **Visual Studio Code** or Visual Studio 2022
- ✅ **Shared Storage Setup**: `C:\ManteqStorage_Shared\CmsDocuments\`

## ⚙️ Configuration

### **�️ Production Storage Configuration**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=CmsDatabase_Dev;Integrated Security=true;Trust Server Certificate=true"
  },
  "FileStorage": {
    "Path": "C:\\ManteqStorage_Shared\\CmsDocuments"  // Shared with TMS
  }
}
```

### **🗄️ Database Setup**
The CMS uses a shared database with all Manteq services:
```sql
-- Database created automatically on first startup
-- Shared by: CMS, TMS, Email Service
USE CmsDatabase_Dev

-- Tables:
-- Documents (managed by CMS)
-- Templates (managed by TMS, references Documents)
```

### **📁 Storage Directory Structure**
```
C:\ManteqStorage_Shared\
└── CmsDocuments\                 # All documents stored here
    ├── email-doc-test_xyz.docx   # Direct uploads
    ├── Template_abc123.docx      # TMS registered templates
    └── document_def456.pdf       # Various document types
```

## 🏃‍♂️ Quick Start

### **1. Setup Storage Directory**
```powershell
# Create shared storage (run once for entire Manteq system)
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\CmsDocuments" -Force
```

### **2. Build and Run**
```powershell
# Navigate to CMS project
cd CMS.WebApi

# Restore packages and build
dotnet restore
dotnet build

# Run the CMS service
dotnet run
```

### **3. Access the API**
- 🌐 **Swagger UI**: http://localhost:5000/swagger
- 🔗 **Base URL**: http://localhost:5000
- ✅ **Health Check**: http://localhost:5000/api/documents/health

### **4. Verify Installation**
```powershell
# Test health endpoint
curl http://localhost:5000/api/documents/health

# Expected response: {"status": "healthy", "service": "CMS"}
```

> 🎉 **That's it!** CMS is running and ready to store documents for the entire Manteq system.

## 🌐 API Endpoints

### **📄 Document Registration**
```http
POST http://localhost:5000/api/documents/register
Content-Type: multipart/form-data

Parameters:
- file: Document file (required)
- description: Document description (optional)
```

**Example Response**:
```json
{
  "documentId": "ced4e35b-134c-4002-bed1-de26d3dabe89",
  "fileName": "Email_Template.docx",
  "message": "Document registered successfully",
  "downloadUrl": "/api/documents/ced4e35b-134c-4002-bed1-de26d3dabe89/download"
}
```

### **📄 Document Metadata**
```http
GET http://localhost:5000/api/documents/{documentId}
```

**Example Response**:
```json
{
  "id": "ced4e35b-134c-4002-bed1-de26d3dabe89",
  "fileName": "Email_Template.docx",
  "filePath": "C:\\ManteqStorage_Shared\\CmsDocuments\\Email_Template.docx_ced4e35b-134c-4002-bed1-de26d3dabe89.docx",
  "description": "Customer email template",
  "createdAt": "2025-09-11T13:38:34.123Z",
  "fileSize": 42060
}
```

### **📥 Document Download**
```http
GET http://localhost:5000/api/documents/{documentId}/download
```
Returns the actual file with appropriate content-type headers.

### **🔍 Health Check**
```http
GET http://localhost:5000/api/documents/health
```

**Response**:
```json
{
  "status": "healthy",
  "service": "CMS",
  "timestamp": "2025-09-11T13:45:00Z",
  "database": "connected",
  "storage": "accessible"
}
```

## 🗂️ Project Structure

```
CMS.WebApi/
├── 📄 appsettings.json              # Production configuration
├── 📄 appsettings.Development.json  # Development settings
├── 📄 Program.cs                    # Application entry point & DI setup
├── 
├── 📁 Controllers/
│   └── DocumentsController.cs       # REST API endpoints
├── 📁 Data/
│   └── CmsDbContext.cs             # Entity Framework context
├── 📁 Models/
│   ├── Document.cs                 # Document entity model
│   └── DocumentDto.cs              # Data transfer objects
├── 📁 Services/
│   ├── IDocumentService.cs         # Service interface
│   └── DocumentService.cs          # Business logic implementation
├── 📁 Properties/
│   └── launchSettings.json         # Development launch settings
└── 📁 bin/Debug/net9.0/            # Build output (DLL for integration)
```

### **🔧 Key Configuration Files**
- **Program.cs**: Dependency injection, Entity Framework setup, CORS configuration
- **appsettings.json**: Database connection, file storage path, logging levels  
- **CmsDbContext.cs**: Database models, relationships, Entity Framework configuration
- **DocumentService.cs**: Core business logic for file storage and database operations

### **📊 Database Context**
```csharp
public class CmsDbContext : DbContext
{
    public DbSet<Document> Documents { get; set; }
    
    // Shared database with TMS:
    // - Documents table (managed by CMS)
    // - Templates table (managed by TMS, references Documents)
}
```

## �️ Development and Deployment

### **� Development Setup**
```powershell
# Clone and build
git clone https://github.com/SalehShalab87/Manteq-doc-system.git
cd Manteq-doc-system\CMS.WebApi

# Create storage directory
New-Item -ItemType Directory -Path "C:\ManteqStorage_Shared\CmsDocuments" -Force

# Build and run
dotnet build
dotnet run
```

### **🚀 Production Deployment**
```powershell
# Build release version
dotnet publish -c Release -o ./publish

# Configure production settings in appsettings.json:
# - Update database connection string
# - Set production storage path
# - Configure logging levels
```

### **🧩 Use as Service in Other Projects**
```csharp
// In other Manteq services (like TMS):
services.AddScoped<IDocumentService, DocumentService>();
services.AddDbContext<CmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Services can now inject and use CMS functionality
public class TemplateService 
{
    private readonly IDocumentService _documentService;
    
    public TemplateService(IDocumentService documentService)
    {
        _documentService = documentService;
    }
    
    // Use CMS to store template files
    public async Task<Document> StoreTemplateAsync(IFormFile file)
    {
        return await _documentService.CreateAsync(file, "Template file");
    }
}
```

### **📦 DLL Integration**
- **Build Output**: `bin\Release\net9.0\CMS.WebApi.dll`
- **Dependencies**: Entity Framework Core, ASP.NET Core
- **Usage**: Reference project or DLL in other Manteq services

## 🧪 Testing the CMS

### **🔍 Health Check Test**
```powershell
# Verify CMS is running
curl http://localhost:5000/api/documents/health

# Expected: {"status": "healthy", "service": "CMS"}
```

### **📄 Document Upload Test**
```powershell
# Upload a document
curl -X POST "http://localhost:5000/api/documents/register" `
     -F "file=@test-document.docx" `
     -F "description=Test document upload"

# Response includes documentId for further operations
```

### **📥 Document Download Test**
```powershell
# Download the uploaded document
curl -X GET "http://localhost:5000/api/documents/{documentId}/download" `
     --output downloaded-document.docx
```

### **🌐 Swagger UI Testing**
1. Navigate to http://localhost:5000/swagger
2. Use **POST /api/documents/register** to upload a file
3. Copy the returned `documentId`
4. Use **GET /api/documents/{documentId}** to get metadata
5. Use **GET /api/documents/{documentId}/download** to download

### **📊 Storage Verification**
```powershell
# Check that files are stored correctly
Get-ChildItem "C:\ManteqStorage_Shared\CmsDocuments\"

# Should show uploaded files with GUID naming pattern
```

## � Integration with Other Services

### **🎯 TMS Integration (Primary Use Case)**
The Template Management System uses CMS internally for template storage:

```csharp
// TMS calls CMS services internally
// When you POST to TMS /api/templates/register:
//   1. TMS receives template file
//   2. TMS calls CMS DocumentService internally  
//   3. CMS stores file in shared storage
//   4. CMS returns Document ID to TMS
//   5. TMS creates Template record with CmsDocumentId foreign key
```

**Integration Flow:**
```
TMS Template Upload
        ↓
TMS → CMS.DocumentService.CreateAsync()
        ↓
CMS stores in C:\ManteqStorage_Shared\CmsDocuments\
        ↓
CMS returns Document ID
        ↓
TMS creates Template record
```

### **📧 Email Service Integration**
Email Service can attach CMS documents to outgoing emails:

```http
POST /api/email/send-with-attachments
{
  "to": ["recipient@example.com"],
  "subject": "Documents Attached",
  "body": "Please find documents attached",
  "cmsDocumentIds": ["ced4e35b-134c-4002-bed1-de26d3dabe89"]
}
```

### **🗄️ Shared Database Schema**
```sql
-- CmsDatabase_Dev
Documents (CMS)                Templates (TMS)
├── Id (PK)            ←──────── CmsDocumentId (FK)
├── FileName                     ├── Id (PK)
├── FilePath                     ├── Name
├── Description                  ├── Description
├── CreatedAt                    └── CreatedAt
└── FileSize
```

### **📁 Storage Architecture**
All services share the same storage locations but CMS manages the files:
- **CMS**: Creates and manages files in `CmsDocuments/`
- **TMS**: Processes files from `CmsDocuments/`, outputs to `TmsGenerated/`
- **Email**: References files from both locations as needed

## 📚 Additional Resources

### **🌐 API Documentation**
- **Swagger UI**: http://localhost:5000/swagger (when running)
- **OpenAPI Spec**: Available at runtime for integration tools

### **🔗 Related Documentation**
- **[Main System README](../README.md)** - Complete Manteq Document System overview
- **[TMS README](../TMS.WebApi/README.md)** - Template Management System (uses CMS)
- **[Email Service README](../EmailService.WebApi/README.md)** - Email integration
- **[Team Developer Guide](../TEAM_GUIDE.md)** - Comprehensive development guide

### **🧪 Testing Tools**
- **Postman**: Import OpenAPI spec for complete API testing
- **curl**: Command-line testing examples shown above
- **Swagger UI**: Built-in testing interface
- **Integration Tests**: Use CMS as DLL in test projects

---

## 📞 Support and Contact

- **👨‍💻 Lead Developer**: Saleh Shalab
- **📧 Email**: salehshalab2@gmail.com
- **🌐 Repository**: https://github.com/SalehShalab87/Manteq-doc-system
- **🐛 Issues**: Use GitHub Issues for bug reports

---

## ✅ Status: Production Ready

🎉 **CMS Web API is fully operational and production-ready!**

**✅ Features Complete:**
- Document storage and retrieval
- Shared storage architecture  
- Database integration with TMS
- REST API with comprehensive documentation
- Error handling and validation
- Health monitoring

**🔗 Integration Status:**
- ✅ TMS Integration: Fully implemented and tested
- ✅ Email Service Integration: Ready for document attachments
- ✅ Database Schema: Stable and optimized
- ✅ Shared Storage: Production-grade file management

**🚀 Ready for production deployment as the foundation storage layer of the Manteq Document System.**
