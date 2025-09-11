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
        private readonly string _storagePath;
        private const long MaxFileSize = 50 * 1024 * 1024; // 50MB

        public DocumentService(CmsDbContext context, IConfiguration configuration, ILogger<DocumentService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _storagePath = _configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            
            // Ensure storage directory exists
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
                _logger.LogInformation("Created storage directory: {StoragePath}", _storagePath);
            }
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

                // Create new document entity
                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Author = request.Author,
                    Type = request.Type,
                    Size = request.Content.Length,
                    CreationDate = DateTime.UtcNow
                };

                // Generate file name using metadata name and extension from original file
                var fileExtension = Path.GetExtension(request.Content.FileName);
                var fileName = $"{document.Name}_{document.Id}{fileExtension}";
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

                _logger.LogInformation("Document registered successfully: {DocumentId}, Name: {Name}, Size: {Size}",
                    document.Id, document.Name, document.Size);

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
                    .FirstOrDefaultAsync(d => d.Id == id);

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
                        Author = document.Author,
                        CreationDate = document.CreationDate,
                        Type = document.Type,
                        Size = document.Size,
                        DownloadUrl = "" // Empty string indicates download not available
                    };
                }

                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7000";
                var downloadUrl = $"{baseUrl}/api/documents/{id}/download";

                return new RetrieveDocumentResponse
                {
                    Id = document.Id,
                    Name = document.Name,
                    Author = document.Author,
                    CreationDate = document.CreationDate,
                    Type = document.Type,
                    Size = document.Size,
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
                .FirstOrDefaultAsync(d => d.Id == id);

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
                    .OrderByDescending(d => d.CreationDate)
                    .ToListAsync();

                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7000";
                
                var response = documents.Select(document => new RetrieveDocumentResponse
                {
                    Id = document.Id,
                    Name = document.Name,
                    Author = document.Author,
                    CreationDate = document.CreationDate,
                    Type = document.Type,
                    Size = document.Size,
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
