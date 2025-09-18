using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMS.WebApi.Services;

namespace TMS.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CleanupController : ControllerBase
    {
        private readonly ILogger<CleanupController> _logger;
        private readonly IDocumentGenerationService _generationService;
        private const string TmsGeneratedPath = @"C:\\ManteqStorage_Shared\\TmsGenerated";

        public CleanupController(ILogger<CleanupController> logger, IDocumentGenerationService generationService)
        {
            _logger = logger;
            _generationService = generationService;
        }

        [HttpPost("cleanup-tmsgenerated")]
        public async Task<IActionResult> CleanupTmsGeneratedFolder()
        {
            if (!Directory.Exists(TmsGeneratedPath))
            {
                return NotFound($"Directory not found: {TmsGeneratedPath}");
            }

            var files = Directory.GetFiles(TmsGeneratedPath);
            int deleted = 0;
            foreach (var file in files)
            {
                try
                {
                    var result = await _generationService.CleanupGeneratedDocumentByFilePathAsync(file);
                    if (result) deleted++;
                    else _logger.LogWarning("Failed to cleanup file via service: {File}", file);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file: {File}", file);
                }
            }

            return Ok(new { message = $"Deleted {deleted} files from {TmsGeneratedPath}", deletedFiles = deleted });
        }
    }
}
