# 📁 CMS Web API - Content Management System

**Centralized data gateway and document storage service** for the Manteq Document System. Built with ASP.NET Core 9.0, PostgreSQL, and Entity Framework Core.

> 🎯 **Role**: Acts as the **single source of truth** for all system data - documents, templates, and email templates. All other services access data through CMS HTTP APIs.

---

## 🏗️ Architecture Role

The CMS serves as the **Data Gateway** in the microservices architecture:

```
┌─────────────────────────────────────────────────────────┐
│                MANTEQ DOCUMENT SYSTEM                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📧 Email Service ────► 🎯 TMS API ────► 📁 CMS API    │
│  (Stateless)           (Stateless)      (Data Gateway)  │
│                                                         │
│  • HTTP Client         • HTTP Client    • PostgreSQL   │
│  • No Database         • No Database    • File Storage │
│  • MailKit/SMTP        • LibreOffice    • Entity FW    │
│                                                         │
└─────────────────────────────────────────────────────────┘
                                                │
                                    ┌───────────┴───────────┐
                                    │   CMS OWNED DATA      │
                                    ├───────────────────────┤
                                    │ 🗄️ PostgreSQL DB     │
                                    │ 📁 File Storage       │
                                    │ 📊 4 Tables           │
                                    └───────────────────────┘
```

### **Data Gateway Pattern**
- ✅ **Single Database**: CMS owns PostgreSQL database
- ✅ **HTTP APIs**: TMS and EmailService access data via REST
- ✅ **File Storage**: CMS manages all file operations
- ✅ **Data Isolation**: Services don't share database connections

---

## 🚀 Key Features

### **📁 Document Management**
- ✅ **CRUD Operations**: Create, read, update, delete documents
- ✅ **File Storage**: Configurable file storage location
- ✅ **Metadata Tracking**: Type, size, extension, created by, timestamps
- ✅ **Soft Delete**: Trash system with restore capability
- ✅ **Activation Control**: Enable/disable documents
- ✅ **Type Filtering**: Filter by document type with counts

### **📧 Email Template Management**
- ✅ **Body Source Types**: Plain text, TMS template, custom HTML/XHTML
- ✅ **Multiple Attachments**: CMS documents, TMS-generated, custom files
- ✅ **Template Analytics**: Sent/failure counts, success rate
- ✅ **Custom Templates**: Upload HTML/XHTML/MHTML files
- ✅ **Category System**: Organize templates by category
- ✅ **File Management**: Upload/download custom templates and attachments

### **📄 CMS Templates** (for TMS integration)
- ✅ **Template Metadata**: Name, description, category
- ✅ **Placeholder Tracking**: List of template properties
- ✅ **Document References**: Foreign key to CMS documents
- ✅ **Template Types**: Document, TOB, Quotation
- ✅ **Export Formats**: Original, Word, PDF
- ✅ **Usage Analytics**: Success/failure counts

### **🗑️ Trash System**
- ✅ **Unified Trash**: Documents, templates, email templates
- ✅ **Soft Delete**: Recoverable deletion with metadata
- ✅ **Restore Capability**: Undo deletions
- ✅ **Permanent Delete**: Hard delete from system
- ✅ **Empty Trash**: Bulk permanent deletion

---

## 📋 Prerequisites

- ✅ **.NET 9.0 SDK**
- ✅ **PostgreSQL 16+** (Docker or local installation)
- ✅ **Visual Studio Code** or **Visual Studio 2022**

---

## ⚙️ Configuration

### **🔒 Database Connection**

**PostgreSQL Connection** (via `appsettings.json` or environment variables):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=cms_database;Username=cms_user;Password=your_password"
  }
}
```

**Docker Environment Variables**:
```bash
POSTGRES_HOST=postgres
POSTGRES_PORT=5432
POSTGRES_DB=cms_database
POSTGRES_USER=cms_user
POSTGRES_PASSWORD=ManteqCMS@2025
POSTGRES_SSL_MODE=Prefer
```

### **📁 File Storage Configuration**

```json
{
  "FileStorage": {
    "Path": "/app/storage/CmsDocuments"  // Docker
    // OR
    "Path": "C:\\ManteqStorage\\CmsDocuments"  // Windows
  }
}
```

### **🌐 CORS Configuration**

```json
{
  "AllowedOrigins": [
    "http://localhost:4200",  // Angular Dev
    "http://localhost:4201"   // Angular Prod
  ]
}
```

---

## 🗄️ Database Schema

### **Tables Overview**

```sql
📊 CMS Database (cms_database)
├── documents                      -- All documents and files
├── templates                      -- TMS template metadata
├── email_templates                -- Email template definitions
└── email_template_attachments     -- Email attachment configs
```

### **1. Documents Table**

```sql
documents
├── id (uuid, PK)
├── name (varchar 255)
├── type (varchar 50)              -- Invoice, Contract, Report, etc.
├── size (bigint)
├── extension (varchar 10)
├── mime_type (varchar 100)
├── file_path (varchar 500)
├── creation_date (timestamp)
├── is_active (boolean)
├── is_deleted (boolean)
├── deleted_at (timestamp)
├── deleted_by (varchar 100)
└── created_by (varchar 100)
```

**Indexes**: name, type, creation_date, is_active, is_deleted, created_by, extension

### **2. Templates Table**

```sql
templates
├── id (uuid, PK)
├── name (varchar 255)
├── description (varchar 1000)
├── category (varchar 100)
├── cms_document_id (uuid, FK → documents.id)
├── placeholders (jsonb)           -- Array of placeholder names
├── template_type (int)            -- 0=Document, 1=TOB, 2=Quotation
├── default_export_format (int)    -- 0=Original, 1=Word, 4=PDF
├── is_active (boolean)
├── is_deleted (boolean)
├── deleted_at (timestamp)
├── deleted_by (varchar 100)
├── created_at (timestamp)
├── created_by (varchar 100)
├── updated_at (timestamp)
├── updated_by (varchar 100)
├── success_count (int)
└── failure_count (int)
```

**Indexes**: name, category, cms_document_id, is_active, is_deleted, template_type

### **3. Email Templates Table**

```sql
email_templates
├── id (uuid, PK)
├── name (varchar 255)
├── subject (varchar 500)
├── html_content (text)
├── plain_text_content (text)
├── template_id (uuid, FK → templates.id)
├── body_source_type (int)         -- 0=PlainText, 1=TmsTemplate, 2=CustomTemplate
├── tms_template_id (uuid)
├── custom_template_file_path (varchar 500)
├── is_active (boolean)
├── is_deleted (boolean)
├── deleted_at (timestamp)
├── deleted_by (varchar 100)
├── category (varchar 100)
├── sent_count (int)
├── failure_count (int)
├── created_by (varchar 100)
└── created_date (timestamp)
```

**Indexes**: is_active, is_deleted, category, created_by, template_id, body_source_type, tms_template_id

### **4. Email Template Attachments Table**

```sql
email_template_attachments
├── id (uuid, PK)
├── email_template_id (uuid, FK → email_templates.id)
├── source_type (int)              -- 1=CmsDocument, 2=TmsTemplate, 3=CustomFile
├── cms_document_id (uuid, FK → documents.id)
├── tms_template_id (uuid)
├── tms_export_format (int)
├── custom_file_path (varchar 500)
├── custom_file_name (varchar 255)
├── file_size (bigint)
├── mime_type (varchar 100)
├── display_order (int)
├── created_date (timestamp)
└── created_by (varchar 100)
```

**Indexes**: email_template_id, source_type, display_order

---

## 🏃‍♂️ Quick Start

### **1. Start PostgreSQL (Docker)**

```bash
docker run -d \
  --name manteq-postgres \
  -e POSTGRES_DB=cms_database \
  -e POSTGRES_USER=cms_user \
  -e POSTGRES_PASSWORD=ManteqCMS@2025 \
  -p 5432:5432 \
  postgres:16-alpine
```

### **2. Update Configuration**

Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=cms_database;Username=cms_user;Password=ManteqCMS@2025"
  },
  "FileStorage": {
    "Path": "C:\\ManteqStorage\\CmsDocuments"
  }
}
```

### **3. Create Storage Directory**

```powershell
# Windows
New-Item -ItemType Directory -Path "C:\ManteqStorage\CmsDocuments" -Force

# Linux/Mac
mkdir -p /app/storage/CmsDocuments
```

### **4. Run CMS**

```bash
cd CMS.Webapi
dotnet restore
dotnet run
```

**Access Points**:
- 🌐 API: `http://localhost:5000`
- 📖 Swagger: `http://localhost:5000/swagger`
- ✅ Health: `http://localhost:5000/health`

---

## 🌐 API Endpoints

### **📁 Documents API**

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/documents/register` | Upload and register document |
| GET | `/api/documents/{id}` | Get document metadata |
| GET | `/api/documents/{id}/download` | Download document file |
| GET | `/api/documents` | List all documents (with filters) |
| POST | `/api/documents/{id}/activate` | Activate document |
| POST | `/api/documents/{id}/deactivate` | Deactivate document |
| DELETE | `/api/documents/{id}` | Soft delete (move to trash) |
| GET | `/api/documents/types` | Get document types with counts |

### **📧 Email Templates API**

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/email-templates` | Create email template |
| GET | `/api/email-templates/{id}` | Get email template |
| GET | `/api/email-templates` | List all email templates |
| PUT | `/api/email-templates/{id}` | Update email template |
| DELETE | `/api/email-templates/{id}` | Soft delete email template |
| POST | `/api/email-templates/{id}/activate` | Activate email template |
| POST | `/api/email-templates/{id}/deactivate` | Deactivate email template |
| GET | `/api/email-templates/{id}/analytics` | Get template analytics |
| GET | `/api/email-templates/categories` | Get all categories |
| GET | `/api/email-templates/{id}/custom-template` | Download custom template |
| POST | `/api/email-templates/{id}/upload-custom` | Upload custom template file |
| GET | `/api/email-templates/{id}/attachments` | Get template attachments |
| GET | `/api/email-templates/{id}/attachments/{index}/download` | Download attachment |
| POST | `/api/email-templates/{id}/increment-sent` | Increment sent count |
| POST | `/api/email-templates/{id}/increment-failure` | Increment failure count |

### **📄 Templates API** (for TMS)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/templates` | Create template |
| GET | `/api/templates/{id}` | Get template by ID |
| GET | `/api/templates` | List templates (with filters) |
| PUT | `/api/templates/{id}` | Update template |
| DELETE | `/api/templates/{id}` | Soft delete template |
| POST | `/api/templates/{id}/activate` | Activate template |
| POST | `/api/templates/{id}/deactivate` | Deactivate template |
| POST | `/api/templates/{id}/increment-success` | Increment success count |
| POST | `/api/templates/{id}/increment-failure` | Increment failure count |

### **🗑️ Trash API**

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/trash` | Get all deleted items |
| POST | `/api/trash/documents/{id}/restore` | Restore document |
| POST | `/api/trash/templates/{id}/restore` | Restore template |
| POST | `/api/trash/email-templates/{id}/restore` | Restore email template |
| DELETE | `/api/trash/documents/{id}/permanent` | Permanently delete document |
| DELETE | `/api/trash/templates/{id}/permanent` | Permanently delete template |
| DELETE | `/api/trash/email-templates/{id}/permanent` | Permanently delete email template |
| DELETE | `/api/trash/empty` | Empty entire trash |

---

## 📝 API Examples

### **Document Registration**

```http
POST /api/documents/register
Content-Type: multipart/form-data

name=Invoice-2025-001
type=Invoice
Content=@invoice.pdf
```

**Response:**
```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "message": "Document registered successfully"
}
```

### **Email Template Creation**

```http
POST /api/email-templates
Content-Type: application/json

{
  "name": "Welcome Email",
  "subject": "Welcome to Manteq",
  "bodySourceType": 1,
  "tmsTemplateId": "template-guid-here",
  "category": "Onboarding",
  "attachments": [
    {
      "sourceType": 1,
      "cmsDocumentId": "doc-guid-here",
      "displayOrder": 0
    }
  ]
}
```

### **Get Trash Items**

```http
GET /api/trash
```

**Response:**
```json
{
  "documents": [
    {
      "id": "doc-id",
      "name": "Old Invoice",
      "type": "Document",
      "deletedAt": "2025-01-15T10:30:00Z",
      "deletedBy": "user@example.com",
      "originalType": "Invoice"
    }
  ],
  "templates": [],
  "emailTemplates": []
}
```

---

## 🐳 Docker Deployment

### **Dockerfile** (provided)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CMS.Webapi/CMS.WebApi.csproj", "CMS.Webapi/"]
RUN dotnet restore "CMS.Webapi/CMS.WebApi.csproj"
COPY . .
WORKDIR "/src/CMS.Webapi"
RUN dotnet build "CMS.WebApi.csproj" -c Release -o /app/build
RUN dotnet publish "CMS.WebApi.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
RUN mkdir -p /app/storage/CmsDocuments
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "CMS.WebApi.dll"]
```

### **Run with Docker Compose**

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: cms_database
      POSTGRES_USER: cms_user
      POSTGRES_PASSWORD: ManteqCMS@2025
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data

  cms-api:
    build:
      context: .
      dockerfile: CMS.Webapi/Dockerfile
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=cms_database;Username=cms_user;Password=ManteqCMS@2025
      - FileStorage__Path=/app/storage/CmsDocuments
    ports:
      - "5000:5000"
    volumes:
      - cms-storage:/app/storage
    depends_on:
      - postgres

volumes:
  postgres-data:
  cms-storage:
```

---

## 🧪 Testing

### **Health Check**

```bash
curl http://localhost:5000/health

# Response
{
  "status": "healthy",
  "service": "CMS API",
  "version": "v1",
  "timestamp": "2025-11-18T10:00:00Z"
}
```

### **Document Upload Test**

```bash
curl -X POST http://localhost:5000/api/documents/register \
  -F "name=Test Document" \
  -F "type=Invoice" \
  -F "Content=@test.pdf"
```

### **Email Template Test**

```bash
curl -X POST http://localhost:5000/api/email-templates \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Template",
    "subject": "Test Subject",
    "bodySourceType": 0,
    "plainTextContent": "Test email body",
    "category": "Testing"
  }'
```

---

## 📊 Monitoring & Analytics

### **Template Analytics**

```bash
curl http://localhost:5000/api/email-templates/{id}/analytics

# Response
{
  "templateId": "guid",
  "templateName": "Welcome Email",
  "sentCount": 1250,
  "failureCount": 15,
  "totalAttempts": 1265,
  "successRate": 98.81
}
```

### **Document Types Summary**

```bash
curl http://localhost:5000/api/documents/types

# Response
[
  { "type": "Invoice", "count": 523 },
  { "type": "Contract", "count": 145 },
  { "type": "Report", "count": 89 }
]
```

---

## 🔧 Development

### **Project Structure**

```
CMS.Webapi/
├── Controllers/
│   ├── DocumentsController.cs         # Document CRUD operations
│   ├── EmailTemplatesController.cs    # Email template management
│   ├── TemplatesController.cs         # CMS templates for TMS
│   └── TrashController.cs             # Soft delete management
├── Data/
│   └── CmsDbContext.cs                # Entity Framework context
├── Models/
│   ├── Document.cs                    # Document entity
│   ├── EmailTemplate.cs               # Email template entity
│   ├── EmailTemplateAttachment.cs     # Attachment entity
│   ├── Template.cs                    # CMS template entity
│   └── *Dto.cs                        # Data transfer objects
├── Services/
│   ├── DocumentService.cs             # Document business logic
│   ├── EmailTemplateService.cs        # Email template logic
│   ├── CmsTemplateService.cs          # Template logic
│   └── EmailTemplateFileService.cs    # File operations
├── Program.cs                         # Application entry point
├── appsettings.json                   # Configuration
└── Dockerfile                         # Docker configuration
```

### **Adding New Features**

1. **Create Entity** in `Models/`
2. **Add DbSet** to `CmsDbContext`
3. **Create Service** interface and implementation
4. **Add Controller** with API endpoints
5. **Run Migration**: `dotnet ef migrations add YourMigration`
6. **Update Database**: `dotnet ef database update`

---

## 🔒 Security

### **Authentication Headers**

The API expects user identification via headers:

```http
X-SME-UserId: user@example.com
```

This header is used for:
- Document `created_by` field
- Audit logging
- Soft delete `deleted_by` field

### **File Upload Validation**

- **Max file size**: 50MB (configurable)
- **Allowed types**: All common document types
- **Path sanitization**: Prevents directory traversal
- **GUID naming**: Avoids filename conflicts

---

## 📞 Support

- **Repository**: https://github.com/SalehShalab87/Manteq-doc-system
- **Lead Developer**: Saleh Shalab
- **Email**: salehshalab2@gmail.com

---

## ✅ Production Ready

🎉 **CMS Web API is fully operational and production-ready!**

**✅ Core Features**:
- PostgreSQL database with Entity Framework Core
- Complete CRUD for documents, templates, email templates
- Soft delete with trash management
- File storage with configurable location
- RESTful API with Swagger documentation
- Docker support with health checks
- Analytics and monitoring endpoints

**✅ Microservices Integration**:
- HTTP API client for TMS and EmailService
- Stateless service architecture
- Resilient communication with retry policies
- Independent deployment and scaling

🚀 **Ready to serve as the data gateway for the entire Manteq Document System!**