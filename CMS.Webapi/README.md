# 📁 CMS Web API - Content Management System

A Content Management System (CMS) Web API built with ASP.NET Core 9.0 and Entity Framework Core for document storage and retrieval. **Part of the Manteq Document System** - provides core document storage services for the Template Management System (TMS).

## 🏗️ Role in Manteq Document System

The CMS serves as the **foundational data layer** for the complete document automation platform:

```
Document Automation Flow:
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   CMS API   │───▶│   TMS API   │───▶│   Output    │
│  (Storage)  │    │ (Processing)│    │ (Generated) │
└─────────────┘    └─────────────┘    └─────────────┘
      ▲                    │                  │
      │              ┌─────▼─────┐            │
      │              │ LibreOffice│            │
      │              │ Conversion │            │
      │              └───────────┘            │
      │                                       │
      └───────────── Document Storage ◀───────┘
```

## 🚀 Features

- **Document Registration**: Upload and store documents with metadata
- **Document Retrieval**: Get document metadata and download URLs  
- **File Storage**: Configurable disk-based storage with SQL Server metadata
- **RESTful API**: Clean REST endpoints with comprehensive Swagger documentation
- **File Size Limits**: 50MB maximum file size with validation
- **Multiple File Types**: Support for documents, images, archives, and more
- **TMS Integration**: Provides storage services for Template Management System

## 📋 Prerequisites

- .NET 9.0 SDK
- SQL Server Express (SQLEXPRESS instance)
- Visual Studio Code or Visual Studio

## ⚙️ Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=SALEH-PC\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;Trust Server Certificate=True;"
  },
  "FileStorage": {
    "Path": "C:\\Temp\\CMS_Storage"
  },
  "BaseUrl": "https://localhost:7000"
}
```

## 🏃‍♂️ Running the Application

1. **Clone and navigate to the project**:
   ```bash
   cd CMS.WebApi
   ```

2. **Restore packages**:
   ```bash
   dotnet restore
   ```

3. **Build the project**:
   ```bash
   dotnet build
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

5. **Access the API**:
   - Swagger UI: https://localhost:7276
   - HTTP: http://localhost:5077

## 📚 API Endpoints

### 📄 Register Document
- **POST** `/api/documents/register`
- **Content-Type**: `multipart/form-data`
- **Parameters**:
  - `Name` (string, required): Document name
  - `Author` (string, required): Document author
  - `Type` (string, required): Document type
  - `Content` (file, required): File content

**Response**:
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "message": "Document registered successfully"
}
```

### 📄 Retrieve Document
- **GET** `/api/documents/{id}`
- **Parameters**:
  - `id` (guid, required): Document ID

**Response**:
```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "document.pdf",
  "author": "John Doe",
  "creationDate": "2025-09-10T10:30:00Z",
  "type": "PDF",
  "size": 1048576,
  "downloadUrl": "https://localhost:7276/api/documents/{id}/download"
}
```

### 📄 Download Document
- **GET** `/api/documents/{id}/download`
- **Parameters**:
  - `id` (guid, required): Document ID

**Response**: File download with appropriate content-type

## 🗂️ Project Structure

```
CMS.WebApi/
├── Controllers/
│   └── DocumentsController.cs      # API endpoints
├── Data/
│   └── CmsDbContext.cs            # Entity Framework context
├── Models/
│   ├── Document.cs                # Document entity
│   └── DocumentDto.cs             # Data transfer objects
├── Services/
│   ├── IDocumentService.cs        # Service interface
│   └── DocumentService.cs         # Service implementation
├── Properties/
│   └── launchSettings.json        # Launch configuration
├── appsettings.json              # Application settings
├── appsettings.Development.json  # Development settings
└── Program.cs                    # Application entry point
```

## 🔧 Database Setup

The application uses Entity Framework Core with SQL Server. To set up the database:

1. **Create the database** (manually or through EF migrations):
   ```sql
   CREATE DATABASE CMS_Database;
   ```

2. **Enable database auto-creation** in Program.cs:
   ```csharp
   // Uncomment these lines in Program.cs
   using (var scope = app.Services.CreateScope())
   {
       var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
       context.Database.EnsureCreated();
   }
   ```

## 📁 File Storage

- **Default Location**: `C:\Temp\CMS_Storage`
- **Configurable**: Set `FileStorage:Path` in appsettings.json
- **Naming Convention**: `{DocumentName}_{DocumentId}.{Extension}`
- **Auto-Creation**: Storage directory is created automatically

## 🧪 Testing with Swagger UI

1. Navigate to https://localhost:7276
2. Use the **Register Document** endpoint to upload a file
3. Copy the returned document ID
4. Use **Retrieve Document** to get metadata
5. Use **Download Document** to download the file

## 🚀 Usage as DLL

This CMS can be used as a DLL by other projects:

1. **Build the project**:
   ```bash
   dotnet build -c Release
   ```

2. **Reference the DLL** in other projects:
   ```xml
   <ProjectReference Include="..\\CMS.WebApi\\CMS.WebApi.csproj" />
   ```

3. **Use the services** in your application:
   ```csharp
   services.AddScoped<IDocumentService, DocumentService>();
   ```

## � TMS Integration

This CMS is designed to work seamlessly with the **Template Management System (TMS)**:

### **As Storage Backend**
```csharp
// TMS uses CMS services internally
services.AddScoped<IDocumentService, DocumentService>();
services.AddScoped<ICmsTemplateService, CmsTemplateService>();
```

### **Template Storage Flow**
1. **Upload templates** to CMS via `/api/documents/register`
2. **TMS references** CMS documents for template processing
3. **Generated documents** can be stored back in CMS
4. **Clean separation** - TMS processes, CMS stores

### **Security Model**
- **CMS endpoints**: Used internally by TMS
- **TMS endpoints**: Exposed to external clients
- **Controller exclusion**: TMS hides CMS controllers from public API

## �📝 Next Steps

- [x] Set up database auto-creation
- [x] Integration with Template Management System (TMS)
- [x] Clean API separation and security
- [ ] Add authentication/authorization
- [ ] Implement document versioning
- [ ] Add search and filtering capabilities
- [ ] Create unit and integration tests

## 🤝 Integration with TMS

For complete document automation capabilities, pair this CMS with the **Template Management System (TMS)**:

- **CMS**: Handles document storage and retrieval
- **TMS**: Processes templates and generates documents  
- **Together**: Provide end-to-end document automation

See the main [README.md](../README.md) for complete system documentation.

## 🤝 Team Distribution

- **Build Location**: `bin\Release\net9.0\CMS.WebApi.dll`
- **NuGet Package**: Can be packaged for team distribution
- **API Documentation**: Available via Swagger at runtime

---

**Status**: ✅ **CMS Web API is ready for testing!**

The API is running and ready for integration with TMS and EmailService systems.
