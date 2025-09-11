# Manteq Document System - Complete Overview

A comprehensive document management and automation system consisting of three integrated Web APIs: CMS (Content Management System), TMS (Template Management System), and Email Service.

## 🏗️ System Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Email Service │    │       TMS       │    │       CMS       │
│    Web API      │────│    Web API      │────│    Web API      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                    ┌─────────────────┐
                    │   SQL Server    │
                    │    Database     │
                    └─────────────────┘
```

## 🔧 System Components

### 1. CMS (Content Management System)
**Port**: 5000 (HTTP) / 7000 (HTTPS)
- **Purpose**: Document storage and retrieval
- **Key Features**:
  - Document registration and storage
  - File retrieval and management
  - Template management for CMS
  - Direct file upload/download

### 2. TMS (Template Management System) 
**Port**: 5020 (HTTP) / 7020 (HTTPS)
- **Purpose**: Template-based document generation
- **Key Features**:
  - Template registration with placeholder extraction
  - Document generation with property injection
  - Multiple export formats (Word, PDF, HTML, EmailHtml)
  - Document embedding (templates within templates)
  - LibreOffice integration for format conversion

### 3. Email Service
**Port**: 5030 (HTTP) / 7030 (HTTPS)
- **Purpose**: Email automation with document integration
- **Key Features**:
  - Send emails with TMS-generated content
  - Attach CMS documents to emails
  - Multiple SMTP account support
  - EmailHtml format for embedded email content
  - Smart attachment handling

## 📋 Complete API Reference

### CMS APIs
- `POST /api/documents/register` - Upload and register documents
- `GET /api/documents/{id}` - Retrieve document information
- `GET /api/documents/{id}/download` - Download document file
- `GET /api/documents` - List all documents
- `POST /api/cms-templates/register` - Register CMS templates
- `GET /api/cms-templates/{id}` - Retrieve CMS template
- `GET /api/cms-templates` - List CMS templates

### TMS APIs
- `POST /api/templates/register` - Register new template
- `GET /api/templates/{id}` - Retrieve template information
- `GET /api/templates/{id}/properties` - Get template properties/placeholders
- `GET /api/templates` - List all templates
- `POST /api/generation/generate` - Generate document from template
- `GET /api/generation/{id}/download` - Download generated document
- `POST /api/embedding/generate-with-embeddings` - Generate with embedded templates

### Email Service APIs
- `POST /api/email/send-with-template` - Send email with TMS template
- `POST /api/email/send-with-documents` - Send email with CMS attachments
- `GET /api/email/accounts` - Get configured email accounts
- `GET /api/email/health` - Health check

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 or later
- SQL Server (LocalDB or full instance)
- LibreOffice (for document conversion)
- Valid Outlook email account with App Password

### Quick Start
1. **Clone the repository**
   ```bash
   git clone https://github.com/SalehShalab87/Manteq-doc-system.git
   cd Manteq-doc-system
   ```

2. **Build the complete solution**
   ```bash
   dotnet build ManteqDocumentSystem.sln
   ```

3. **Start all services** (in separate terminals):
   ```bash
   # Terminal 1: CMS
   cd CMS.Webapi
   dotnet run
   
   # Terminal 2: TMS
   cd TMS.WebApi
   dotnet run
   
   # Terminal 3: Email Service
   cd EmailService.WebApi
   dotnet run
   ```

4. **Access the APIs**:
   - **CMS**: http://localhost:5000
   - **TMS**: http://localhost:5020  
   - **Email Service**: http://localhost:5030

## 📖 Usage Examples

### Complete Workflow Example

#### 1. Register a Template (TMS)
```bash
POST http://localhost:5020/api/templates/register
Content-Type: multipart/form-data

{
  "Name": "Invoice Template",
  "Description": "Professional invoice template",
  "Category": "Financial",
  "CreatedBy": "Admin",
  "TemplateFile": [invoice-template.docx]
}
```

#### 2. Send Email with Generated Document
```bash
POST http://localhost:5030/api/email/send-with-template
Content-Type: application/json

{
  "toRecipients": ["customer@example.com"],
  "subject": "Your Invoice",
  "templateId": "550e8400-e29b-41d4-a716-446655440000",
  "propertyValues": {
    "CustomerName": "John Doe",
    "InvoiceNumber": "INV-2023-001",
    "Amount": "$1,250.00",
    "DueDate": "2023-12-31"
  },
  "exportFormat": "EmailHtml"
}
```

### Integration Patterns

#### Pattern 1: Document Storage + Email
```bash
# 1. Store document in CMS
POST /api/documents/register

# 2. Send email with stored document
POST /api/email/send-with-documents
{
  "cmsDocumentIds": ["document-id"],
  "subject": "Document Attached"
}
```

#### Pattern 2: Template Generation + Email  
```bash
# 1. Register template in TMS
POST /api/templates/register

# 2. Generate and email in one step
POST /api/email/send-with-template
{
  "templateId": "template-id",
  "propertyValues": {...},
  "exportFormat": "Pdf"
}
```

## ⚙️ Configuration

### Database Configuration (Environment Variables)
All services now use environment variables for database connection. Create `.env` files in each project directory:

```env
# Database settings (same for all three services)
DB_SERVER=YOUR_SERVER\\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true
```

### Email Configuration
Configure SMTP settings in `EmailService.WebApi/appsettings.json`:
```json
{
  "Email": {
    "Smtp": {
      "Host": "smtp-mail.outlook.com",
      "Port": 587,
      "EnableSsl": true
    },
    "Accounts": [
      {
        "Name": "primary",
        "DisplayName": "Your Display Name",
        "EmailAddress": "your-email@example.com",
        "IsDefault": true
      }
    ]
  }
}
```

Create `.env` file in EmailService.WebApi:
```env
# Database settings
DB_SERVER=YOUR_SERVER\\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true

# SMTP settings
SMTP_HOST=smtp-mail.outlook.com
SMTP_PORT=587
SMTP_SSL=true
SMTP_USERNAME=your-email@example.com
SMTP_PASSWORD=your-app-password
```

### TMS Configuration  
Configure document generation settings in `TMS.WebApi/appsettings.json`:
```json
{
  "TmsSettings": {
    "RetentionHours": 24,
    "CleanupIntervalMinutes": 30,
    "MaxFileSizeMB": 100
  }
}
```

## 🔧 Advanced Features

### Email Service Smart Behavior
- **EmailHtml Export**: Generated content becomes the email body
- **Other Formats**: Generated document is attached, custom body is preserved
- **Auto-cleanup**: Generated documents are deleted after email sending

### TMS Document Embedding
Generate complex documents by embedding multiple templates:
```json
{
  "mainTemplateId": "main-template-id",
  "mainTemplateValues": {...},
  "embeddings": [
    {
      "embedTemplateId": "sub-template-id",
      "embedTemplateValues": {...},
      "embedPlaceholder": "{{SubDocument}}"
    }
  ]
}
```

### Multi-Format Support
- **Original**: Keep source format (Word/Excel/PowerPoint)
- **Word**: Convert to .docx
- **HTML**: Standard HTML with external images  
- **EmailHtml**: HTML with base64 embedded images
- **PDF**: Convert to PDF using LibreOffice

## 🔍 Monitoring & Health Checks

All services provide health endpoints:
- **CMS**: `GET /api/documents/health`
- **TMS**: `GET /api/templates/health`  
- **Email**: `GET /api/email/health`

## 📊 Solution Files

### Main Solution (Complete System)
```bash
dotnet build ManteqDocumentSystem.sln
```
Includes: CMS + TMS + Email Service

### Individual Solutions
```bash
# CMS only
dotnet build CMS.Webapi/CMS.WebApi.sln

# TMS with CMS dependency  
dotnet build TMS.WebApi/TMS.WebApi.sln

# Email Service with all dependencies
dotnet build EmailService.WebApi/EmailService.WebApi.sln
```

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection**
   - Verify SQL Server is running
   - Check connection string format
   - Ensure database is created

2. **LibreOffice Not Found**
   - Install LibreOffice
   - Verify installation path in TMS logs

3. **Email Authentication**
   - Use App Password for Outlook
   - Enable 2FA on Microsoft account
   - Check firewall settings for SMTP port 587

4. **Port Conflicts**
   - Default ports: CMS(5000), TMS(5020), Email(5030)
   - Modify in `launchSettings.json` if needed

### Logs
All services provide detailed logging:
- **Development**: Console + Debug output
- **Production**: File logging (configurable)

## 📝 Contributing

This system follows clean architecture principles:
- **Controllers**: HTTP request handling
- **Services**: Business logic
- **Models**: Data transfer objects
- **Integration**: Cross-service communication

## 📄 License

Part of the Manteq Document System.

---

**Developed by Saleh Shalab** - [salehshalab2@gmail.com](mailto:salehshalab2@gmail.com)

## 🎯 Next Steps

The system is now complete and production-ready. Potential enhancements:
- Authentication & Authorization
- API rate limiting
- Advanced email templates
- Document versioning
- Audit logging
- Containerization (Docker)
- API documentation portal
- Webhook notifications
- Batch processing
- Cloud storage integration

---

**System Status**: ✅ **COMPLETE & READY FOR PRODUCTION**
