using Microsoft.AspNetCore.Mvc;
using CMS.WebApi.Models;
using CMS.WebApi.Services;

namespace CMS.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplatesController : ControllerBase
    {
        private readonly ICmsTemplateService _templateService;
        private readonly ILogger<TemplatesController> _logger;

        public TemplatesController(ICmsTemplateService templateService, ILogger<TemplatesController> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new CMS template
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Template>> CreateTemplate([FromBody] Template template)
        {
            try
            {
                _logger.LogInformation("Creating template: {TemplateName}", template.Name);
                
                var createdTemplate = await _templateService.CreateTemplateAsync(template);
                
                return Ok(createdTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get template by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Template>> GetTemplate(Guid id)
        {
            try
            {
                var template = await _templateService.GetTemplateByIdAsync(id);
                
                if (template == null)
                {
                    return NotFound(new { error = $"Template with ID {id} not found" });
                }

                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving template {TemplateId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Template>>> GetAllTemplates(
            [FromQuery] string? category = null,
            [FromQuery] string? name = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                List<Template> templates;
                if (!string.IsNullOrEmpty(name))
                {
                    templates = await _templateService.GetTemplatesByNameAsync(name);
                }
                else if (!string.IsNullOrEmpty(category))
                {
                    templates = await _templateService.GetTemplatesByCategoryAsync(category);
                }
                else if (isActive.HasValue && isActive.Value)
                {
                    templates = await _templateService.GetActiveTemplatesAsync();
                }
                else
                {
                    templates = await _templateService.GetAllTemplatesAsync();
                }

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        ///<summary>
        /// Get template placeholders by name
        /// </summary>
        [HttpGet("placeholders")]
        public async Task<ActionResult<List<string>>> GetTemplatePlaceholders([FromQuery] string name, [FromQuery] bool isActive = true)
        {
            try
            {
                var placeholders = await _templateService.GetTemplatePlaceholdersAsync(name, isActive);
                return Ok(placeholders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving placeholders for template {TemplateName}", name);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update template
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Template>> UpdateTemplate(Guid id, [FromBody] Template template)
        {
            try
            {
                var updatedTemplate = await _templateService.UpdateTemplateAsync(id, template);
                
                if (updatedTemplate == null)
                {
                    return NotFound(new { error = $"Template with ID {id} not found" });
                }

                return Ok(updatedTemplate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template {TemplateId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Activate template
        /// </summary>
        [HttpPost("{id}/activate")]
        public async Task<ActionResult> ActivateTemplate(Guid id)
        {
            try
            {
                var result = await _templateService.ActivateTemplateAsync(id);
                
                if (!result)
                {
                    return NotFound(new { error = $"Template with ID {id} not found" });
                }

                return Ok(new { message = "Template activated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating template {TemplateId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deactivate template
        /// </summary>
        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult> DeactivateTemplate(Guid id)
        {
            try
            {
                var result = await _templateService.DeactivateTemplateAsync(id);
                
                if (!result)
                {
                    return NotFound(new { error = $"Template with ID {id} not found" });
                }

                return Ok(new { message = "Template deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating template {TemplateId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Delete template (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTemplate(Guid id)
        {
            try
            {
                var result = await _templateService.DeleteTemplateAsync(id);
                
                if (!result)
                {
                    return NotFound(new { error = $"Template with ID {id} not found" });
                }

                return Ok(new { message = "Template deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template {TemplateId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Increment success count for template
        /// </summary>
        [HttpPost("{id}/increment-success")]
        public async Task<ActionResult> IncrementSuccessCount(Guid id)
        {
            try
            {
                var result = await _templateService.IncrementSuccessCountAsync(id);
                
                if (!result)
                {
                    return NotFound(new { error = $"Template with ID {id} not found" });
                }

                return Ok(new { message = "Success count incremented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing success count for template {TemplateId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Increment failure count for template
        /// </summary>
        [HttpPost("{id}/increment-failure")]
        public async Task<ActionResult> IncrementFailureCount(Guid id)
        {
            try
            {
                var result = await _templateService.IncrementFailureCountAsync(id);
                
                if (!result)
                {
                    return NotFound(new { error = $"Template with ID {id} not found" });
                }

                return Ok(new { message = "Failure count incremented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing failure count for template {TemplateId}", id);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
