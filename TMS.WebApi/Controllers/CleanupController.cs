using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

namespace TMS.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CleanupController : ControllerBase
    {
        private readonly ILogger<CleanupController> _logger;
        private const string TmsGeneratedPath = @"C:\\ManteqStorage_Shared\\TmsGenerated";

        public CleanupController(ILogger<CleanupController> logger)
        {
            _logger = logger;
        }

        [HttpPost("cleanup-tmsgenerated")]
        public IActionResult CleanupTmsGeneratedFolder()
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
                    System.IO.File.Delete(file);
                    deleted++;
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, $"Failed to delete file: {file}");
                }
            }

            return Ok(new { message = $"Deleted {deleted} files from {TmsGeneratedPath}", deletedFiles = deleted });
        }
    }
}
