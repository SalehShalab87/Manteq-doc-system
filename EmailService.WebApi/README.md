# Email Service Web API

A powerful Email Service that integrates with TMS (Template Management System) and CMS (Content Management System) for advanced email automation with template-based content generation and document attachments.

## 🚀 Features

### 📧 Two Main APIs
- **POST /api/email/send-with-template** - Send emails with TMS-generated content
- **POST /api/email/send-with-documents** - Send emails with CMS document attachments
- **GET /api/email/accounts** - Get available email accounts
- **GET /api/email/health** - Health check endpoint

### ⚙️ Configuration Features
- ✅ Multiple email accounts with default selection
- ✅ SMTP credentials stored securely in environment variables
- ✅ Outlook SMTP integration
- ✅ Configurable account settings

### 🎯 Smart Template Handling
- ✅ **EmailHtml Export**: Replaces email body content (no attachment)
- ✅ **Other Formats**: Keeps body content + adds document as attachment
- ✅ **Auto-cleanup**: Generated documents deleted immediately after sending

### 📎 Attachment Support
- ✅ **TMS Integration**: Generate documents on-the-fly from templates
- ✅ **CMS Integration**: Attach existing documents from CMS
- ✅ **Mixed Support**: Can use both in different emails

### 🔐 Security & Validation
- ✅ Simple email address validation
- ✅ SMTP credentials in environment variables
- ✅ No sensitive data exposed in configuration files

## 📋 Prerequisites

- .NET 9.0 or later
- SQL Server (existing CMS database)
- TMS Web API (for template generation)
- CMS Web API (for document management)
- Valid Outlook email account with App Password

## 🛠️ Setup Instructions

### 1. Clone and Build
```bash
# Navigate to the EmailService.WebApi directory
cd EmailService.WebApi

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### 2. Configure SMTP Credentials
Create a `.env` file from the template:
```bash
# Copy the template
cp .env.template .env

# Edit .env with your actual credentials
SMTP_HOST=smtp-mail.outlook.com
SMTP_PORT=587
SMTP_SSL=true
SMTP_USERNAME=your-email@outlook.com
SMTP_PASSWORD=your-app-password
```

### 3. Update Email Accounts
Edit `appsettings.json` and `appsettings.Development.json` to configure your email accounts:
```json
{
  "Email": {
    "Accounts": [
      {
        "Name": "primary",
        "DisplayName": "Your Display Name",
        "EmailAddress": "your-email@outlook.com",
        "IsDefault": true
      }
    ]
  }
}
```

### 4. Run the Service
```bash
# Development mode
dotnet run

# Or production mode
dotnet run --launch-profile "https"
```

The service will be available at:
- **Swagger UI**: `https://localhost:7000/` (or your configured port)
- **HTTP**: `http://localhost:5000/` (or your configured port)

## 📖 API Usage Examples

### Send Email with TMS Template (EmailHtml)
```json
POST /api/email/send-with-template
{
  "toRecipients": ["customer@example.com"],
  "subject": "Your Invoice",
  "templateId": "550e8400-e29b-41d4-a716-446655440000",
  "propertyValues": {
    "CustomerName": "John Doe",
    "InvoiceNumber": "INV-2023-001",
    "Amount": "$1,250.00"
  },
  "exportFormat": "EmailHtml"
}
```

### Send Email with TMS Template (as Attachment)
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
}
```

### Send Email with CMS Documents
```json
POST /api/email/send-with-documents
{
  "toRecipients": ["client@example.com"],
  "ccRecipients": ["manager@example.com"],
  "subject": "Project Documents",
  "htmlBody": "<p>Please review the attached project documents.</p>",
  "cmsDocumentIds": [
    "550e8400-e29b-41d4-a716-446655440000",
    "550e8400-e29b-41d4-a716-446655440001"
  ]
}
```

## 🔧 Configuration Reference

### Email Account Configuration
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
        "Name": "account-identifier",
        "DisplayName": "Display Name for Emails",
        "EmailAddress": "email@domain.com",
        "IsDefault": true
      }
    ]
  }
}
```

### Environment Variables
- `SMTP_HOST`: SMTP server hostname
- `SMTP_PORT`: SMTP server port (usually 587)
- `SMTP_SSL`: Enable SSL/TLS (true/false)
- `SMTP_USERNAME`: Your email address
- `SMTP_PASSWORD`: Your App Password (not regular password)

## 📚 Integration Notes

### TMS Integration
The Email Service automatically integrates with your existing TMS Web API to:
- Generate documents from templates
- Handle property value injection
- Support multiple export formats
- Clean up generated files after sending

### CMS Integration
The Email Service integrates with your existing CMS Web API to:
- Retrieve documents by ID
- Attach multiple documents to emails
- Handle various file types with proper MIME types

### Export Format Behavior
- **EmailHtml**: Generated content replaces the email body (no attachment)
- **Word/Pdf/Html**: Generated document is attached, provided body content is used
- **Original**: Uses the original template format as attachment

## 🔍 Troubleshooting

### Common Issues

1. **SMTP Authentication Failed**
   - Ensure you're using an App Password, not your regular password
   - Verify the username is your full email address
   - Check that 2FA is enabled on your Outlook account

2. **TMS/CMS Integration Issues**
   - Verify that TMS and CMS services are running
   - Check database connection strings match between services
   - Ensure project references are correct

3. **Email Not Sending**
   - Check the logs for detailed error messages
   - Verify recipient email addresses are valid
   - Confirm SMTP settings are correct

### Generating Outlook App Password
1. Go to https://account.live.com/proofs/manage/additional
2. Click "Create a new app password"
3. Use this password in your `.env` file

## 📊 Health Check
Use the health endpoint to verify the service is running:
```
GET /api/email/health
```

## 🏗️ Architecture

The Email Service follows a clean architecture pattern:
- **Controllers**: Handle HTTP requests and responses
- **Services**: Business logic and email processing
- **Integration Services**: Interface with TMS and CMS
- **Models**: Data transfer objects and configuration

## 📝 License

This project is part of the Manteq Document System.

---

**Developed by Saleh Shalab** - [salehshalab2@gmail.com](mailto:salehshalab2@gmail.com)
