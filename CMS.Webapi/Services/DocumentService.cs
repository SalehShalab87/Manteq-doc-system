using Microsoft.EntityFrameworkCore;
using CMS.WebApi.Data;
using CMS.WebApi.Models;

namespace CMS.WebApi.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly CmsDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _storagePath;
        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

        public DocumentService(
            CmsDbContext context, 
            IConfiguration configuration, 
            ILogger<DocumentService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _storagePath = _configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            
            // Ensure storage directory exists
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
                _logger.LogInformation("Created storage directory: {StoragePath}", _storagePath);
            }
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["X-SME-UserId"].FirstOrDefault() 
                   ?? "SYSTEM";
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                "pdf" => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "doc" => "application/msword",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "xls" => "application/vnd.ms-excel",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "ppt" => "application/vnd.ms-powerpoint",
                "txt" => "text/plain",
                "csv" => "text/csv",
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                "zip" => "application/zip",
                "rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }

        public async Task<RegisterDocumentResponse> RegisterDocumentAsync(RegisterDocumentRequest request)
        {
            try
            {
                // Validate file size
                if (request.Content.Length > MaxFileSize)
                {
                    throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB");
                }

                if (request.Content.Length == 0)
                {
                    throw new ArgumentException("File content cannot be empty");
                }

                // Extract extension from filename
                var extension = Path.GetExtension(request.Content.FileName).TrimStart('.');
                
                // Determine MimeType
                var mimeType = GetMimeType(extension);

                // Create new document entity
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Type = request.Type ?? "General",
                    Size = request.Content.Length,
                    Extension = extension,
                    MimeType = mimeType,
                    CreationDate = DateTime.UtcNow,
                    IsActive = true,
                    CreatedBy = GetCurrentUserId()
                };

                // Generate file name: {Name}_{Id}.{extension}
                var fileName = $"{document.Name}_{document.Id}.{extension}";
                var filePath = Path.Combine(_storagePath, fileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Content.CopyToAsync(stream);
                }

                // Update document with file path
                document.FilePath = filePath;

                // Save document metadata to database
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document registered successfully: {DocumentId}, Name: {Name}, Size: {Size}, CreatedBy: {CreatedBy}",
                    document.Id, document.Name, document.Size, document.CreatedBy);

                return new RegisterDocumentResponse
                {
                    Id = document.Id,
                    Message = "Document registered successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering document: {Name}", request.Name);
                throw;
            }
        }

        public async Task<RetrieveDocumentResponse?> RetrieveDocumentAsync(Guid id)
        {
            try
            {
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (document == null)
                {
                    _logger.LogWarning("Document not found: {DocumentId}", id);
                    return null;
                }

                // Verify file still exists - graceful handling for production
                if (!File.Exists(document.FilePath))
                {
                    _logger.LogWarning("Document file not found on disk - file may have been deleted: {FilePath}", document.FilePath);
                    
                    // Return metadata but indicate file is unavailable
                    return new RetrieveDocumentResponse
                    {
                        Id = document.Id,
                        Name = document.Name + " [FILE MISSING]",
                        Type = document.Type,
                        Size = document.Size,
                        Extension = document.Extension,
                        MimeType = document.MimeType,
                        CreationDate = document.CreationDate,
                        IsActive = document.IsActive,
                        CreatedBy = document.CreatedBy,
                        DownloadUrl = "" // Empty string indicates download not available
                    };
                }

                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7000";
                var downloadUrl = $"{baseUrl}/api/documents/{id}/download";

                return new RetrieveDocumentResponse
                {
                    Id = document.Id,
                    Name = document.Name,
                    Type = document.Type,
                    Size = document.Size,
                    Extension = document.Extension,
                    MimeType = document.MimeType,
                    CreationDate = document.CreationDate,
                    IsActive = document.IsActive,
                    CreatedBy = document.CreatedBy,
                    DownloadUrl = downloadUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document: {DocumentId}", id);
                throw;
            }
        }

        public async Task<string> GetDocumentFilePathAsync(Guid id)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (document == null)
            {
                throw new FileNotFoundException("Document not found in database");
            }

            if (!File.Exists(document.FilePath))
            {
                _logger.LogWarning("Document file missing from disk: {DocumentId} - {FilePath}", id, document.FilePath);
                throw new FileNotFoundException($"Document file not found on disk: {document.Name}");
            }

            return document.FilePath;
        }

        public async Task<List<RetrieveDocumentResponse>> GetAllDocumentsAsync()
        {
            try
            {
                var documents = await _context.Documents
                    .Where(d => !d.IsDeleted)
                    .OrderByDescending(d => d.CreationDate)
                    .ToListAsync();

                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7000";
                
                var response = documents.Select(document => new RetrieveDocumentResponse
                {
                    Id = document.Id,
                    Name = document.Name,
                    Type = document.Type,
                    Size = document.Size,
                    Extension = document.Extension,
                    MimeType = document.MimeType,
                    CreationDate = document.CreationDate,
                    IsActive = document.IsActive,
                    CreatedBy = document.CreatedBy,
                    DownloadUrl = $"{baseUrl}/api/documents/{document.Id}/download"
                }).ToList();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all documents");
                throw;
            }
        }
    }
}
