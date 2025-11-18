# 📄 Manteq Document System

**Production-ready microservices platform** for document management, template processing, and email automation. Built with ASP.NET Core 9.0, PostgreSQL, and Docker.

> 🔥 **Status**: ✅ **PRODUCTION READY** - Fully tested microservices architecture with stateless services and data gateway pattern.

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    MANTEQ DOCUMENT SYSTEM                       │
│                     (Microservices Architecture)                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📧 Email Service ────► 🎯 TMS API ────► 📁 CMS API            │
│  Port: 5030            Port: 5267       Port: 5000             │
│  (Orchestrator)        (Generator)      (Data Gateway)          │
│                                                                 │
│  • MailKit/SMTP       • LibreOffice     • PostgreSQL           │
│  • HTTP Client        • OpenXML         • File Storage         │
│  • Stateless          • HTTP Client     • Entity Framework     │
│  • Polly Resilience   • Stateless       • REST APIs            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                                    │
                        ┌───────────┴────────────┐
                        │   SHARED RESOURCES     │
                        ├────────────────────────┤
                        │ 🗄️ PostgreSQL (CMS)   │
                        │ 📁 File Storage (CMS)  │
                        │ 🔄 Auto-Cleanup (TMS)  │
                        └────────────────────────┘
```

### **🎯 Architecture Highlights**

| Service | Role | Database | Storage | Key Technology |
|---------|------|----------|---------|----------------|
| **CMS** | Data Gateway | ✅ PostgreSQL | ✅ File System | Entity Framework Core |
| **TMS** | Document Generator | ❌ HTTP Client | 🔄 Temporary (15min) | LibreOffice + OpenXML |
| **Email** | Email Orchestrator | ❌ HTTP Client | ❌ None | MailKit + Polly |

---

## 🚀 Service Overview

### **📁 CMS (Content Management System)** - Port 5000

**Role**: Central data gateway and document storage

**Responsibilities**:
- ✅ PostgreSQL database ownership (4 tables)
- ✅ Document CRUD operations
- ✅ Email template management
- ✅ CMS template metadata for TMS
- ✅ File storage management
- ✅ Soft delete/trash system
- ✅ Analytics tracking

**Key Endpoints**:
```
POST   /api/documents/register
GET    /api/documents/{id}
POST   /api/email-templates
GET    /api/templates
GET    /api/trash
```

**Technology Stack**:
- ASP.NET Core 9.0
- Entity Framework Core
- PostgreSQL 16
- File System Storage

📖 **[Full CMS Documentation →](CMS.Webapi/README.md)**

---

### **🎯 TMS (Template Management System)** - Port 5267

**Role**: Stateless document generation engine

**Responsibilities**:
- ✅ Template registration via CMS
- ✅ Placeholder extraction (OpenXML)
- ✅ Document generation from templates
- ✅ Multi-format export (Word, PDF, HTML, EmailHtml)
- ✅ LibreOffice conversion
- ✅ Base64 image embedding for EmailHtml
- ✅ Auto-cleanup (15-minute retention)
- ✅ Excel workflow support

**Key Endpoints**:
```
POST   /api/templates/register
POST   /api/templates/generate
POST   /api/templates/generate?autoDownload=true
GET    /api/templates/{id}/properties
POST   /api/templates/generate-with-embeddings
```

**Technology Stack**:
- ASP.NET Core 9.0
- OpenXML SDK
- LibreOffice
- EPPlus (Excel)
- HTTP Client (CMS)

📖 **[Full TMS Documentation →](TMS.WebApi/README.md)**

---

### **📧 Email Service** - Port 5030

**Role**: Email orchestration and delivery

**Responsibilities**:
- ✅ Template-based email sending
- ✅ TMS EmailHtml generation integration
- ✅ CMS document attachment retrieval
- ✅ Multi-account SMTP support
- ✅ MailKit email sending
- ✅ Polly resilience (retry + circuit breaker)
- ✅ Analytics tracking via CMS

**Key Endpoints**:
```
POST   /api/email/send-with-template
POST   /api/email/send-with-documents
POST   /api/email/send-tms-html-and-attachment
POST   /api/email/test-template
GET    /api/email/accounts
```

**Technology Stack**:
- ASP.NET Core 9.0
- MailKit/MimeKit
- HTTP Clients (TMS + CMS)
- Polly (Resilience)

📖 **[Full Email Service Documentation →](EmailService.WebApi/README.md)**

---

## 📊 Data Flow Patterns

### **Pattern 1: Email with TMS EmailHtml**

```
User Request
    ↓
Email Service
    ├─► CMS: Get email template config
    ├─► TMS: Generate EmailHtml with base64 images
    ├─► TMS: Auto-cleanup after generation
    └─► SMTP: Send email with HTML body
```

### **Pattern 2: Document Generation**

```
User Request
    ↓
TMS API
    ├─► CMS: Get template metadata
    ├─► CMS: Download template file
    ├─► OpenXML: Extract placeholders
    ├─► OpenXML: Fill data
    ├─► LibreOffice: Convert format
    └─► Return: Generated document (15min retention)
```

### **Pattern 3: Email Template Management**

```
User Request
    ↓
CMS API
    ├─► Database: CRUD operations
    ├─► File Storage: Save custom HTML templates
    ├─► File Storage: Save custom attachments
    └─► Return: Template configuration
```

---

## 🗄️ Database Schema

### **PostgreSQL Database** (Owned by CMS)

```sql
📊 cms_database
├── documents
│   ├── id (uuid, PK)
│   ├── name, type, size, extension
│   ├── file_path, mime_type
│   ├── is_active, is_deleted
│   └── created_by, creation_date
│
├── templates
│   ├── id (uuid, PK)
│   ├── name, description, category
│   ├── cms_document_id (FK → documents)
│   ├── placeholders (jsonb array)
│   ├── template_type, default_export_format
│   ├── success_count, failure_count
│   └── is_active, is_deleted
│
├── email_templates
│   ├── id (uuid, PK)
│   ├── name, subject, html_content
│   ├── body_source_type (PlainText/TmsTemplate/CustomTemplate)
│   ├── tms_template_id, custom_template_file_path
│   ├── sent_count, failure_count
│   └── is_active, is_deleted, category
│
└── email_template_attachments
    ├── id (uuid, PK)
    ├── email_template_id (FK → email_templates)
    ├── source_type (CmsDocument/TmsTemplate/CustomFile)
    ├── cms_document_id (FK → documents)
    ├── tms_template_id, tms_export_format
    └── custom_file_path, display_order
```

**Access Pattern**:
- ✅ **CMS**: Direct database access (Entity Framework)
- ✅ **TMS**: HTTP API calls to CMS
- ✅ **Email**: HTTP API calls to CMS

---

## 📁 File Storage Architecture

### **Storage Locations**

```
Windows:
C:\ManteqStorage\
├── CmsDocuments\      # Permanent (CMS owned)
├── TmsGenerated\      # Temporary (15min, TMS cleanup)
└── TmsTemp\           # Working (immediate cleanup)

Docker:
/app/storage/
├── CmsDocuments/      # Volume: cms-storage
├── TmsGenerated/      # Volume: tms-storage
└── TmsTemp/           # Volume: tms-storage
```

### **Storage Management**

| Location | Retention | Managed By | Purpose |
|----------|-----------|------------|---------|
| `CmsDocuments` | ♾️ Permanent | CMS | Documents, templates |
| `TmsGenerated` | 15 minutes | TMS | Generated docs |
| `TmsTemp` | Immediate | TMS | Processing workspace |

---

## 🚀 Quick Start

### **Prerequisites**

- ✅ **.NET 9.0 SDK**
- ✅ **Docker** & **Docker Compose**
- ✅ **LibreOffice** (for local TMS)
- ✅ **SMTP Account** (Outlook, Gmail)

### **Option 1: Docker Compose (Recommended)**

```bash
# Clone repository
git clone https://github.com/SalehShalab87/Manteq-doc-system.git
cd Manteq-doc-system

# Configure SMTP (create .env or edit docker-compose.yml)
export SMTP_HOST=smtp-mail.outlook.com
export SMTP_PORT=587
export SMTP_USERNAME=your-email@outlook.com
export SMTP_PASSWORD=your-app-password

# Start all services
docker-compose up -d

# Verify services
curl http://localhost:5000/health  # CMS
curl http://localhost:5267/health  # TMS
curl http://localhost:5030/health  # Email
```

**Access Points**:
- 🌐 CMS: `http://localhost:5000` - [Swagger](http://localhost:5000/swagger)
- 🎯 TMS: `http://localhost:5267` - [Swagger](http://localhost:5267/swagger)
- 📧 Email: `http://localhost:5030` - [Swagger](http://localhost:5030/swagger)

### **Option 2: Local Development**

```bash
# 1. Start PostgreSQL
docker run -d \
  --name manteq-postgres \
  -e POSTGRES_DB=cms_database \
  -e POSTGRES_USER=cms_user \
  -e POSTGRES_PASSWORD=ManteqCMS@2025 \
  -p 5432:5432 \
  postgres:16-alpine

# 2. Create storage directories
mkdir -p C:\ManteqStorage\CmsDocuments
mkdir -p C:\ManteqStorage\TmsGenerated
mkdir -p C:\ManteqStorage\TmsTemp

# 3. Start CMS
cd CMS.Webapi
dotnet run

# 4. Start TMS (new terminal)
cd TMS.WebApi
dotnet run

# 5. Start Email Service (new terminal)
cd EmailService.WebApi
dotnet run
```

---

## 🔄 Complete Workflow Example

### **Scenario**: Send email with TMS-generated content and PDF attachment

```bash
# Step 1: Register template in TMS (stores in CMS)
curl -X POST http://localhost:5267/api/templates/register \
  -F "name=Invoice Template" \
  -F "category=Invoices" \
  -F "TemplateFile=@invoice_template.docx"
# Returns: { "templateId": "template-guid", "extractedPlaceholders": [...] }

# Step 2: Create email template in CMS
curl -X POST http://localhost:5000/api/email-templates \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Invoice Email",
    "subject": "Your Invoice {{InvoiceNumber}}",
    "bodySourceType": 1,
    "tmsTemplateId": "template-guid",
    "category": "Invoices"
  }'
# Returns: { "id": "email-template-guid" }

# Step 3: Send email (Email Service orchestrates TMS + SMTP)
curl -X POST http://localhost:5030/api/email/send-tms-html-and-attachment \
  -H "Content-Type: application/json" \
  -d '{
    "toRecipients": ["customer@example.com"],
    "subject": "Your Invoice INV-2025-001",
    "bodyTemplateId": "template-guid",
    "bodyPropertyValues": {
      "CustomerName": "John Smith",
      "InvoiceNumber": "INV-2025-001"
    },
    "attachmentTemplateId": "template-guid",
    "attachmentPropertyValues": {
      "CustomerName": "John Smith",
      "InvoiceNumber": "INV-2025-001",
      "Amount": "1,250.00",
      "DueDate": "2025-12-01"
    },
    "attachmentExportFormat": "Pdf"
  }'
# Result: Email sent with HTML body + PDF attachment
```

---

## 🐳 Docker Compose Configuration

### **docker-compose.yml** (Provided)

```yaml
services:
  # PostgreSQL - CMS Database
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
    healthcheck:
      test: ["CMD-SHELL", "pg_isready"]

  # CMS - Data Gateway
  cms-api:
    build:
      context: .
      dockerfile: CMS.Webapi/Dockerfile
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=cms_database;...
      - FileStorage__Path=/app/storage/CmsDocuments
    ports:
      - "5000:5000"
    volumes:
      - cms-storage:/app/storage
    depends_on:
      - postgres

  # TMS - Document Generator
  tms-api:
    build:
      context: .
      dockerfile: TMS.WebApi/Dockerfile
    environment:
      - CmsApi__BaseUrl=http://cms-api:5000
      - TMS__SharedStoragePath=/app/storage/TmsGenerated
    ports:
      - "5267:5267"
    volumes:
      - tms-storage:/app/storage
    depends_on:
      - cms-api

  # Email - Orchestrator
  email-service:
    build:
      context: .
      dockerfile: EmailService.WebApi/Dockerfile
    environment:
      - Email__Smtp__Host=${SMTP_HOST}
      - Email__Smtp__Username=${SMTP_USERNAME}
      - Email__Smtp__Password=${SMTP_PASSWORD}
      - TmsApi__BaseUrl=http://tms-api:5267
      - CmsApi__BaseUrl=http://cms-api:5000
    ports:
      - "5030:5030"
    depends_on:
      - tms-api
      - cms-api

volumes:
  postgres-data:
  cms-storage:
  tms-storage:
```

---

## 🧪 Testing the System

### **Health Checks**

```bash
# Check all services
curl http://localhost:5000/health  # CMS
curl http://localhost:5267/health  # TMS
curl http://localhost:5030/health  # Email

# Expected: All return {"status": "healthy"}
```

### **Integration Test**

```bash
# 1. Upload document to CMS
curl -X POST http://localhost:5000/api/documents/register \
  -F "name=Test Document" \
  -F "Content=@test.pdf"

# 2. Register template in TMS
curl -X POST http://localhost:5267/api/templates/register \
  -F "name=Test Template" \
  -F "TemplateFile=@template.docx"

# 3. Send email via Email Service
curl -X POST http://localhost:5030/api/email/send-with-template \
  -H "Content-Type: application/json" \
  -d '{ ... }'
```

---

## 📊 Monitoring & Observability

### **Health Endpoints**

```bash
# Service Health
GET http://localhost:5000/health
GET http://localhost:5267/health
GET http://localhost:5030/health

# CMS Analytics
GET http://localhost:5000/api/email-templates/{id}/analytics
GET http://localhost:5000/api/documents/types

# TMS Analytics
GET http://localhost:5267/api/templates/{id}/analytics
```

### **Docker Logs**

```bash
# View logs
docker-compose logs -f cms-api
docker-compose logs -f tms-api
docker-compose logs -f email-service

# Check specific service
docker logs manteq-cms-api
```

---

## 🔒 Security Best Practices

### **Configuration Management**

✅ **DO**:
- Use environment variables for sensitive data
- Use Docker secrets in production
- Store SMTP app passwords (never regular passwords)
- Use `.env` files (add to `.gitignore`)

❌ **DON'T**:
- Commit credentials to Git
- Use regular email passwords
- Hardcode connection strings

### **Authentication Headers**

```http
X-SME-UserId: user@example.com
```

Used for:
- Audit logging
- Created by / Updated by fields
- Soft delete tracking

---

## 📈 Production Deployment

### **Scaling Strategy**

| Service | Scaling | Database | Considerations |
|---------|---------|----------|----------------|
| **CMS** | Vertical | ✅ Shared | Single instance (data gateway) |
| **TMS** | Horizontal | ❌ Stateless | Multiple instances (CPU bound) |
| **Email** | Horizontal | ❌ Stateless | Multiple instances (I/O bound) |

### **Resource Requirements**

**Minimum**:
- CPU: 2 cores per service
- RAM: 2GB per service
- Storage: 20GB for documents
- PostgreSQL: 10GB database

**Recommended**:
- CPU: 4 cores per service
- RAM: 4GB per service
- Storage: 100GB+ for documents
- PostgreSQL: 50GB database

---

## 📞 Support & Resources

### **Documentation**

- 📁 [CMS API Documentation](CMS.Webapi/README.md)
- 🎯 [TMS API Documentation](TMS.WebApi/README.md)
- 📧 [Email Service Documentation](EmailService.WebApi/README.md)

### **API Documentation**

- CMS Swagger: `http://localhost:5000/swagger`
- TMS Swagger: `http://localhost:5267/swagger`
- Email Swagger: `http://localhost:5030/swagger`

### **Contact**

- **Repository**: https://github.com/SalehShalab87/Manteq-doc-system
- **Lead Developer**: Saleh Shalab
- **Email**: salehshalab2@gmail.com
- **Issues**: [GitHub Issues](https://github.com/SalehShalab87/Manteq-doc-system/issues)

---

## ✅ Production Ready Status

🎉 **All services are fully operational and production-ready!**

### **✅ CMS (Data Gateway)**
- PostgreSQL database with Entity Framework
- Document & template management
- Email template system
- Soft delete/trash functionality
- Analytics tracking

### **✅ TMS (Document Generator)**
- Stateless HTTP client architecture
- LibreOffice integration for conversions
- OpenXML document manipulation
- Auto-cleanup system (15min retention)
- Excel workflow support

### **✅ Email Service (Orchestrator)**
- MailKit email sending
- TMS & CMS integration via HTTP
- Polly resilience patterns
- Multi-account SMTP support
- Template-based automation

### **✅ Infrastructure**
- Docker Compose orchestration
- Health checks on all services
- Volume management for persistence
- Network isolation
- Environment-based configuration

---

## 🚀 Next Steps

1. **Review Service Documentation**: Read individual service READMEs
2. **Start with Docker Compose**: Easiest way to get started
3. **Test Workflows**: Try the complete workflow example
4. **Configure SMTP**: Set up email account for Email Service
5. **Review API Documentation**: Explore Swagger endpoints
6. **Deploy to Production**: Follow scaling strategy guidelines

---

**🎊 The Manteq Document System is a production-ready microservices platform that transforms documents, processes templates, and automates emails with professional quality!**