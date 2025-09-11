# 📧 Email Service Web API

**Email automation service** that integrates with TMS and CMS for template-based email generation and document attachments. Built with ASP.NET Core 9.0.

> 📧 **Role**: Email automation hub that combines TMS-generated content with CMS document attachments for comprehensive email workflows.

## 🏗️ Architecture Integration

The Email Service serves as the **communication layer** in the Manteq ecosystem:

```
┌─────────────────────────────────────────────────────────┐
│                MANTEQ DOCUMENT SYSTEM                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📧 Email Service ────── 🎯 TMS API ────── 📁 CMS API   │
│  (Port 5030)           (Port 5267)      (Port 5000)   │
│                                                         │
│  • SMTP Integration    • EmailHtml        • Document    │
│  • Template-based      • Content Gen     • Attachments │
│  • Multi-account       • Base64 images   • File Storage │
│  • Email automation    • Auto-cleanup    • Metadata    │
│                                                         │
└─────────────────────────────────────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    │    EMAIL FLOW     │
                    │                   │
                    │ 1. Request Email  │ → With template ID + data
                    │ 2. Call TMS       │ → Generate EmailHtml content  
                    │ 3. Get HTML       │ → Base64 images embedded
                    │ 4. Send via SMTP  │ → HTML becomes email body
                    │ 5. Cleanup        │ → TMS auto-cleans temp files
                    └───────────────────┘
```

### **🔄 Service Integration Flow**
- **Email → TMS**: Requests EmailHtml generation from templates
- **Email → CMS**: Fetches documents for email attachments
- **TMS → Email**: Returns email-ready HTML with base64 images
- **Email → SMTP**: Sends via configured email accounts

## 🚀 Features

### **📧 Email Automation APIs**
- ✅ **Template-Based Emails**: Generate content from TMS templates on-the-fly
- ✅ **Document Attachments**: Attach files from CMS document storage
- ✅ **Multi-Account Support**: Configure multiple SMTP accounts
- ✅ **Health Monitoring**: Service status and configuration validation

### **🎯 TMS Integration Features**
- ✅ **EmailHtml Format**: TMS content becomes email body (no attachment)
- ✅ **Base64 Images**: All images embedded directly in email HTML
- ✅ **Other Formats**: Word/PDF/HTML added as email attachments
- ✅ **Auto-Cleanup**: Generated documents deleted after email sent
- ✅ **Error Handling**: Graceful fallback when TMS unavailable

### **📁 CMS Integration Features**
- ✅ **Document Attachments**: Attach existing CMS documents to emails
- ✅ **File Metadata**: Include document names and descriptions
- ✅ **Multiple Attachments**: Support for multiple document attachments
- ✅ **File Validation**: Verify documents exist before sending

### **� Security & Configuration**
- ✅ **Environment Variables**: SMTP credentials stored securely
- ✅ **Email Validation**: Basic email address format validation  
- ✅ **SMTP Authentication**: App passwords for secure authentication
- ✅ **Configuration API**: Runtime configuration management

### **⚡ Production Features**
- ✅ **Async Operations**: Non-blocking email sending
- ✅ **Error Logging**: Comprehensive error handling and logging
- ✅ **Health Checks**: Service and dependency monitoring
- ✅ **Scalable Design**: Stateless service ready for horizontal scaling

## 📋 Prerequisites

- ✅ **.NET 9.0 SDK**
- ✅ **SQL Server Express** (shared `CmsDatabase_Dev`)
- ✅ **TMS Web API** running on port 5267
- ✅ **CMS Web API** running on port 5000  
- ✅ **Email Account** with App Password (Outlook/Gmail/etc.)

## ⚙️ Configuration

### **� Environment Variables (Required)**
Create `.env` file in the EmailService.WebApi directory:

**File: `EmailService.WebApi/.env`**
```env
# Database Configuration
DB_SERVER=YOUR_SERVER\SQLEXPRESS
DB_DATABASE=CmsDatabase_Dev  
DB_INTEGRATED_SECURITY=true
DB_TRUST_SERVER_CERTIFICATE=true

# SMTP Configuration
SMTP_HOST=smtp.outlook.com
SMTP_PORT=587
SMTP_USERNAME=your-email@outlook.com
SMTP_PASSWORD=your-app-password
SMTP_ENABLE_SSL=true
```

### **📧 Email Configuration**
```json
// appsettings.json (no database or SMTP credentials here!)
{
  "EmailSettings": {
    "DefaultFromEmail": "noreply@manteq-me.com",
    "DefaultFromName": "Manteq System"
  }
}
```
SMTP_PORT=587
SMTP_USERNAME=your-email@outlook.com  
SMTP_PASSWORD=your-app-password
SMTP_FROM_NAME=Your Name

# Optional: Additional email accounts
# SMTP2_HOST=smtp.gmail.com
# SMTP2_PORT=587
# etc.
```

> **🔒 Security**: SMTP credentials are stored as environment variables, never in code or config files.

## 🏃‍♂️ Quick Start

### **1. Setup Prerequisites**
```powershell
# Ensure TMS and CMS are running
curl http://localhost:5267/api/templates/health  # TMS
curl http://localhost:5000/api/documents/health  # CMS

# Both should return healthy status
```

### **2. Configure SMTP Settings**
```powershell
# Set environment variables for email account
$env:SMTP_HOST = "smtp.outlook.com"
$env:SMTP_PORT = "587"
$env:SMTP_USERNAME = "your-email@outlook.com"
$env:SMTP_PASSWORD = "your-app-password"  # Use App Password, not regular password
$env:SMTP_FROM_NAME = "Your Name"
```

### **3. Build and Run Email Service**
```powershell
# Navigate to Email Service
cd EmailService.WebApi

# Build with TMS and CMS dependencies
dotnet build EmailService.WebApi.sln

# Run the service
dotnet run

# 🌐 Access: http://localhost:5030
# 📖 Swagger: http://localhost:5030/swagger
```

### **4. Test Email Service**
```powershell
# Health check
curl http://localhost:5030/api/email/health

# Expected response:
# {
#   "status": "healthy",
#   "service": "EmailService", 
#   "smtpConfiguration": "configured",
#   "tmsIntegration": "available",
#   "cmsIntegration": "available"
# }
```

### **5. Send Test Email**
```powershell
# Send email with TMS template content
curl -X POST "http://localhost:5030/api/email/send-generated-document" `
     -H "Content-Type: application/json" `
     -d '{
       "to": ["test@example.com"],
       "subject": "Test Email from Manteq System",
       "templateId": "your-template-id",
       "propertyValues": {
         "CustomerName": "Test User"
       },
       "exportFormat": "EmailHtml"
     }'
```

> 🎉 **Success!** Email Service is running and ready to send template-based emails.

## 🌐 API Reference

### **📧 Send Email with TMS Template (EmailHtml)**
**Primary Use Case**: Template content becomes email body
```http
POST http://localhost:5030/api/email/send-generated-document
Content-Type: application/json

{
  "to": ["customer@example.com"],
  "subject": "Your Policy Documents",
  "templateId": "96cec0ae-a1f9-4e01-8e07-16ddd57b4b25",
  "propertyValues": {
    "CustomerName": "John Smith",
    "PolicyNumber": "POL-2025-001234",
    "SupportEmail": "support@manteq-me.com"
  },
  "exportFormat": "EmailHtml"  // Content becomes email body
}
```

**Response:**
```json
{
  "success": true,
  "message": "Email sent successfully",
  "emailId": "abc123-def456-ghi789",
  "recipients": ["customer@example.com"],
  "sentAt": "2025-09-11T14:30:00Z"
}
```

### **📧 Send Email with TMS Template (as Attachment)**
```http
POST http://localhost:5030/api/email/send-generated-document
Content-Type: application/json

{
  "to": ["customer@example.com"],
  "subject": "Your Invoice Document",
  "body": "Please find your invoice attached.",  // Custom body
  "templateId": "invoice-template-id",
  "propertyValues": {
    "CustomerName": "John Smith",
    "InvoiceNumber": "INV-2025-001"
  },
  "exportFormat": "Pdf"  // Creates attachment
}
```

### **📎 Send Email with CMS Document Attachments**
```http
POST http://localhost:5030/api/email/send-with-attachments
Content-Type: application/json

{
  "to": ["customer@example.com"],
  "subject": "Document Delivery",
  "body": "Please find the requested documents attached.",
  "cmsDocumentIds": [
    "ced4e35b-134c-4002-bed1-de26d3dabe89",
    "1d4c5082-021d-42a4-9f39-794671cf8bac"
  ]
}
```

### **🔍 Health and Configuration**
```http
# Service health check
GET http://localhost:5030/api/email/health

# Response:
{
  "status": "healthy",
  "service": "EmailService",
  "timestamp": "2025-09-11T14:30:00Z",
  "smtpConfiguration": "configured",
  "tmsIntegration": "available", 
  "cmsIntegration": "available"
}

# Get email accounts configuration
GET http://localhost:5030/api/email/accounts

# Response:
{
  "accounts": [
    {
      "name": "primary",
      "emailAddress": "noreply@manteq-me.com",
      "displayName": "Manteq System",
      "isDefault": true
    }
  ]
}
```
```json
POST /api/email/send-with-template
{
  "toRecipients": ["customer@example.com"],
  "subject": "Your Invoice Document",
  "htmlBody": "<p>Please find your invoice attached.</p>",
  "templateId": "550e8400-e29b-41d4-a716-446655440000",
  "propertyValues": {
    "CustomerName": "John Doe",
    "InvoiceNumber": "INV-2023-001"
  },
  "exportFormat": "Pdf"
## 🎯 Key Email Workflows

### **📧 EmailHtml Workflow (Primary Use Case)**
```
1. EmailService receives request with templateId + data
     ↓
2. EmailService → TMS: Generate EmailHtml format
     ↓  
3. TMS processes template, creates HTML with base64 images
     ↓
4. EmailService receives HTML content  
     ↓
5. HTML content becomes email body (no attachment)
     ↓
6. Email sent via SMTP, TMS auto-cleans temp files
```

### **📎 Document Attachment Workflow**
```  
1. EmailService receives request with exportFormat = "Pdf"/"Word"
     ↓
2. EmailService → TMS: Generate document in requested format
     ↓
3. TMS creates document file
     ↓
4. EmailService attaches file to email + uses custom body
     ↓
5. Email sent with attachment, temp file cleaned up
```

### **📁 CMS Attachment Workflow**
```
1. EmailService receives CMS document IDs
     ↓
2. EmailService → CMS: Get document metadata & files  
     ↓
3. CMS returns permanent document files
     ↓
4. EmailService attaches documents to email
     ↓
5. Email sent (CMS documents remain permanent)
```

## 🔧 Advanced Configuration

### **📧 Multi-Account SMTP Setup**
```json
{
  "EmailSettings": {
    "DefaultFromEmail": "noreply@manteq-me.com",
    "DefaultFromName": "Manteq System",
    "Accounts": [
      {
        "name": "primary",
        "displayName": "Manteq Support",
        "emailAddress": "support@manteq-me.com",
        "isDefault": true
      },
      {
        "name": "sales", 
        "displayName": "Manteq Sales",
        "emailAddress": "sales@manteq-me.com",
        "isDefault": false
      }
    ]
  }
}
```

### **🔒 Environment Variables Reference**
```env
# Primary SMTP Configuration
SMTP_HOST=smtp.outlook.com
SMTP_PORT=587
SMTP_USERNAME=your-email@outlook.com
SMTP_PASSWORD=your-app-password  # NOT regular password!
SMTP_FROM_NAME=Your Display Name

# Optional: SSL/TLS Settings
SMTP_ENABLE_SSL=true
SMTP_USE_DEFAULT_CREDENTIALS=false
```

### **⚙️ TMS Integration Settings**
```json
{
  "TmsIntegration": {
    "BaseUrl": "http://localhost:5267",
    "TimeoutSeconds": 30,
    "RetryAttempts": 3
  },
  "CmsIntegration": {
    "BaseUrl": "http://localhost:5000", 
    "TimeoutSeconds": 15,
    "RetryAttempts": 2
  }
}
```

## 🧪 Testing the Email Service

### **🔍 Health Check Test**
```powershell
# Verify Email Service and dependencies
curl http://localhost:5030/api/email/health

# Expected response:
# {
#   "status": "healthy",
#   "service": "EmailService",
#   "smtpConfiguration": "configured",
#   "tmsIntegration": "available",
#   "cmsIntegration": "available" 
# }
```

### **📧 Template Email Test**  
```powershell
# Test EmailHtml generation (content becomes email body)
curl -X POST "http://localhost:5030/api/email/send-generated-document" `
     -H "Content-Type: application/json" `
     -d '{
       "to": ["test@example.com"],
       "subject": "Test Template Email",
       "templateId": "your-template-id",
       "propertyValues": {
         "CustomerName": "Test Customer",
         "PolicyNumber": "TEST-001"
       },
       "exportFormat": "EmailHtml"
     }'

# Check email client - content should be formatted HTML with embedded images
```

### **📎 Attachment Test**
```powershell  
# Test document attachment (PDF attached to email)
curl -X POST "http://localhost:5030/api/email/send-generated-document" `
     -H "Content-Type: application/json" `
     -d '{
       "to": ["test@example.com"],
       "subject": "Test Document Attachment",
       "body": "Please find the document attached.",
       "templateId": "your-template-id",
       "propertyValues": {"CustomerName": "Test"},
       "exportFormat": "Pdf"
     }'

# Check email client - should have PDF attachment
```

### **📁 CMS Document Test**
```powershell
# Test CMS document attachment
curl -X POST "http://localhost:5030/api/email/send-with-attachments" `
     -H "Content-Type: application/json" `
     -d '{
       "to": ["test@example.com"],
       "subject": "CMS Document Test", 
       "body": "CMS documents attached.",
       "cmsDocumentIds": ["ced4e35b-134c-4002-bed1-de26d3dabe89"]
     }'
```

## ❌ Troubleshooting

### **🔐 SMTP Authentication Issues**
```powershell
# 1. Check App Password setup (NOT regular password)
# Go to: https://account.live.com/proofs/manage/additional
# Generate new App Password for "Mail" application

# 2. Verify environment variables
echo $env:SMTP_USERNAME  # Should be full email address
echo $env:SMTP_PASSWORD  # Should be App Password (16 characters)
echo $env:SMTP_HOST      # Should be smtp.outlook.com

# 3. Test SMTP connection
curl http://localhost:5030/api/email/health
```

### **🔗 Service Integration Issues**
```powershell
# Verify TMS is running and accessible
curl http://localhost:5267/api/templates/health

# Verify CMS is running and accessible  
curl http://localhost:5000/api/documents/health

# Check Email Service can reach dependencies
curl http://localhost:5030/api/email/health
# Should show tmsIntegration: "available" and cmsIntegration: "available"
```

### **📧 Email Not Sending**
**Common Causes & Solutions:**
- **SMTP Auth Failed** → Use App Password, not regular password
- **Invalid Recipients** → Check email address format validation
- **Template Not Found** → Verify templateId exists in TMS
- **TMS Unavailable** → Ensure TMS service is running
- **Generated Content Too Large** → Check email size limits

### **🔍 Diagnostic Commands**
```powershell
# Check logs for detailed error messages
# Look for SMTP, TMS, or CMS integration errors in console output

# Test individual components
curl http://localhost:5267/api/templates/{templateId}/properties  # TMS
curl http://localhost:5000/api/documents/{documentId}             # CMS
curl http://localhost:5030/api/email/accounts                     # Email config
```

### **� Email Provider Settings**
**Outlook/Hotmail:**
- Host: `smtp.outlook.com`
- Port: `587`
- SSL: `true`
- Auth: App Password required

**Gmail:**
- Host: `smtp.gmail.com`  
- Port: `587`
- SSL: `true`
- Auth: App Password required (2FA must be enabled)

## 🗂️ Project Structure

```
EmailService.WebApi/
├── 📄 appsettings.json              # Email and database configuration
├── 📄 Program.cs                    # Service setup and DI configuration
├── 
├── 📁 Controllers/
│   └── EmailController.cs           # REST API endpoints
├── 📁 Services/
│   ├── EmailService.cs              # Core email sending logic
│   ├── TmsIntegrationService.cs     # TMS API integration
│   └── CmsIntegrationService.cs     # CMS API integration
├── 📁 Models/
│   ├── EmailRequest.cs              # Request models
│   ├── EmailResponse.cs             # Response models  
│   └── EmailConfiguration.cs        # Configuration models
└── 📁 Infrastructure/
    └── EmailServiceExtensions.cs    # DI and service registration
```

## � Additional Resources

### **🔗 Related Documentation**
- **[Main System README](../README.md)** - Complete Manteq Document System overview
- **[TMS README](../TMS.WebApi/README.md)** - Template Management System (Email Service dependency)
- **[CMS README](../CMS.WebApi/README.md)** - Content Management System (Email Service dependency)
- **[Team Developer Guide](../TEAM_GUIDE.md)** - Comprehensive development workflows

### **🌐 API Documentation**
- **Swagger UI**: http://localhost:5030/swagger (when running)
- **OpenAPI Spec**: Available at runtime for integration and testing

### **🧪 Testing Tools**
- **Postman**: Import OpenAPI spec for complete API testing
- **curl**: Command-line examples shown throughout this document
- **Email Clients**: Test actual email delivery and formatting

### **📧 Email Testing Services**
- **Mailtrap**: Test email sending without real delivery
- **MailHog**: Local SMTP testing server
- **Gmail/Outlook**: Use separate test accounts for development

---

## 📞 Support and Contact

- **👨‍💻 Lead Developer**: Saleh Shalab  
- **📧 Email**: salehshalab2@gmail.com
- **🌐 Repository**: https://github.com/SalehShalab87/Manteq-doc-system
- **🐛 Issues**: Use GitHub Issues for bug reports and feature requests

---

## ✅ Status: Production Ready

🎉 **Email Service is fully operational and production-ready!**

**✅ Core Features Complete:**
- Template-based email generation with TMS integration
- EmailHtml format with base64 embedded images  
- CMS document attachment support
- Multi-account SMTP configuration
- Comprehensive error handling and logging

**✅ Integration Status:**
- ✅ TMS Integration: EmailHtml and document attachment generation
- ✅ CMS Integration: Document retrieval and attachment  
- ✅ SMTP Configuration: Multi-provider support with App Passwords
- ✅ Database Access: Shared database with read-only operations
- ✅ Health Monitoring: Complete service and dependency health checks

**🚀 Ready for production deployment as the email automation layer of the Manteq Document System, providing seamless integration between templates, documents, and email delivery.**
