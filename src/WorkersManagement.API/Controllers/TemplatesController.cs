using Microsoft.AspNetCore.Mvc;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure.EmailComposerDtos;

namespace WorkersManagement.API.Controllers
{
    /// <summary>
    /// Manage email templates and rich text content
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplateDesignerService _templateService;
        private readonly ILogger<ITemplateDesignerService> _logger;

        public TemplatesController(ITemplateDesignerService templateService, ILogger<ITemplateDesignerService> logger)
        {
            _templateService = templateService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new email template
        /// </summary>
        /// <param name="request">Template creation data</param>
        /// <returns>Newly created template</returns>
        [HttpPost("create")]
        public async Task<IActionResult> CreateTemplate([FromBody] RichTextTemplateRequestDto request)
        {
            try
            {
                var result = await _templateService.CreateTemplateAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create template");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing template
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <param name="request">Updated template data</param>
        /// <returns>Updated template</returns>
        [HttpPut("update/{templateId}")]
        public async Task<IActionResult> UpdateTemplate(Guid templateId, [FromBody] RichTextTemplateRequestDto request)
        {
            try
            {
                var result = await _templateService.UpdateTemplateAsync(templateId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update template {TemplateId}", templateId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all templates
        /// </summary>
        /// <returns>List of all templates</returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllTemplates()
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            return Ok(templates);
        }

        /// <summary>
        /// Get template by ID
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <returns>Template details</returns>
        [HttpGet("{templateId}")]
        public async Task<IActionResult> GetTemplate(Guid templateId)
        {
            try
            {
                var template = await _templateService.GetTemplateAsync(templateId);
                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get template {TemplateId}", templateId);
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a template
        /// </summary>
        /// <param name="templateId">Template identifier</param>
        /// <returns>Delete result</returns>
        [HttpDelete("{templateId}")]
        public async Task<IActionResult> DeleteTemplate(Guid templateId)
        {
            var result = await _templateService.DeleteTemplateAsync(templateId);
            return result ? Ok(new { message = "Template deleted successfully" })
                         : NotFound(new { error = "Template not found" });
        }

        /// <summary>
        /// Preview template with sample data
        /// </summary>
        /// <param name="request">Preview data</param>
        /// <returns>HTML preview</returns>
        [HttpPost("preview")]
        public IActionResult PreviewTemplate([FromBody] TemplatePreviewRequestDto request)
        {
            try
            {
                var preview = _templateService.GeneratePreview(request);
                return Ok(new { previewHtml = preview });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate preview");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Upload image for template use
        /// </summary>
        /// <param name="file">Image file</param>
        /// <returns>Upload result</returns>
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                var result = await _templateService.UploadTemplateImageAsync(file);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get available font options
        /// </summary>
        /// <returns>List of font options</returns>
        [HttpGet("font-options")]
        public async Task<IActionResult> GetFontOptions()
        {
            var options = await _templateService.GetFontOptionsAsync();
            return Ok(options);
        }

        /// <summary>
        /// Convert HTML content to plain text
        /// </summary>
        /// <param name="request">HTML content to convert</param>
        /// <returns>Plain text version</returns>
        [HttpPost("convert-to-plaintext")]
        public async Task<IActionResult> ConvertToPlainText([FromBody] ConvertToPlainTextRequest request)
        {
            try
            {
                var plainText = await _templateService.ConvertToPlainTextAsync(request.HtmlContent);
                return Ok(new { plainText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert HTML to plain text");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get one-time use templates (non-system templates)
        /// </summary>
        /// <returns>List of one-time templates</returns>
        [HttpGet("one-time-templates")]
        public async Task<IActionResult> GetOneTimeTemplates()
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            var oneTimeTemplates = templates.Where(t => !t.TemplateName.StartsWith("System_")).ToList();
            return Ok(oneTimeTemplates);
        }
    }

    public class ConvertToPlainTextRequest
    {
        public string HtmlContent { get; set; } = string.Empty;
    }
}
