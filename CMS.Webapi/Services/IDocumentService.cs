using CMS.WebApi.Models;

namespace CMS.WebApi.Services
{
    public interface IDocumentService
    {
        Task<RegisterDocumentResponse> RegisterDocumentAsync(RegisterDocumentRequest request);
        Task<RetrieveDocumentResponse?> RetrieveDocumentAsync(Guid id);
        Task<string> GetDocumentFilePathAsync(Guid id);
        Task<List<RetrieveDocumentResponse>> GetAllDocumentsAsync();
    }
}
