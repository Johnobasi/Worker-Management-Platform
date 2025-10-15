using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.EmailComposerDtos;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Core.Repositories
{
    public class TemplateDesignerService : ITemplateDesignerService
    {
        private readonly WorkerDbContext _context;
        private readonly ILogger<TemplateDesignerService> _logger;
        public TemplateDesignerService(WorkerDbContext context, ILogger<TemplateDesignerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RichTextTemplateResponseDto> CreateTemplateAsync(RichTextTemplateRequestDto request)
        {
            try
            {
                var template = new EmailTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = request.TemplateName,
                    Subject = request.Subject,
                    HtmlContent = request.HtmlContent,
                    PlainTextContent = request.PlainTextContent,
                    TemplateType = "RichText",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsOneTimeTemplate = !request.SaveAsTemplate
                };

                template.SetDesignOptions(request.DesignOptions);
                template.SetImages(request.Images);

                await _context.EmailTemplates.AddAsync(template);
                await _context.SaveChangesAsync();

                return new RichTextTemplateResponseDto
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    Subject = template.Subject,
                    HtmlContent = template.HtmlContent,
                    PreviewHtml = GeneratePreview(new TemplatePreviewRequestDto
                    {
                        HtmlContent = template.HtmlContent,
                        DesignOptions = template.GetDesignOptions()
                    }),
                    DesignOptions = template.GetDesignOptions(),
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create template: {TemplateName}", request.TemplateName);
                throw;
            }
        }

        public async Task<RichTextTemplateResponseDto> UpdateTemplateAsync(Guid templateId, RichTextTemplateRequestDto request)
        {
            var template = await _context.EmailTemplates.FindAsync(templateId);
            if (template == null)
                throw new ArgumentException("Template not found");

            template.Name = request.TemplateName;
            template.Subject = request.Subject;
            template.HtmlContent = request.HtmlContent;
            template.PlainTextContent = request.PlainTextContent;
            template.SetDesignOptions(request.DesignOptions);
            template.SetImages(request.Images);
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new RichTextTemplateResponseDto
            {
                TemplateId = template.Id,
                TemplateName = template.Name,
                Subject = template.Subject,
                HtmlContent = template.HtmlContent,
                PreviewHtml = GeneratePreview(new TemplatePreviewRequestDto
                {
                    HtmlContent = template.HtmlContent,
                    DesignOptions = template.GetDesignOptions()
                }),
                DesignOptions = template.GetDesignOptions(),
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };
        }

        public async Task<List<RichTextTemplateResponseDto>> GetAllTemplatesAsync()
        {
            var templates = await _context.EmailTemplates
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync();

            var result = new List<RichTextTemplateResponseDto>();
            foreach (var template in templates)
            {
                result.Add(new RichTextTemplateResponseDto
                {
                    TemplateId = template.Id,
                    TemplateName = template.Name,
                    Subject = template.Subject,
                    HtmlContent = template.HtmlContent,
                    PreviewHtml = GeneratePreview(new TemplatePreviewRequestDto
                    {
                        HtmlContent = template.HtmlContent,
                        DesignOptions = template.GetDesignOptions()
                    }),
                    DesignOptions = template.GetDesignOptions(),
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt
                });
            }

            return result;
        }

        public async Task<RichTextTemplateResponseDto> GetTemplateAsync(Guid templateId)
        {
            var template = await _context.EmailTemplates.FindAsync(templateId);
            if (template == null)
                throw new ArgumentException("Template not found");

            return new RichTextTemplateResponseDto
            {
                TemplateId = template.Id,
                TemplateName = template.Name,
                Subject = template.Subject,
                HtmlContent = template.HtmlContent,
                PreviewHtml = GeneratePreview(new TemplatePreviewRequestDto
                {
                    HtmlContent = template.HtmlContent,
                    DesignOptions = template.GetDesignOptions()
                }),
                DesignOptions = template.GetDesignOptions(),
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            };
        }

        public async Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            var template = await _context.EmailTemplates.FindAsync(templateId);
            if (template == null) return false;

            template.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public string GeneratePreview(TemplatePreviewRequestDto request)
        {
            var styledHtml = ApplyDesignStyles(request.HtmlContent, request.DesignOptions);

            if (request.PlaceholderValues.Any())
            {
                styledHtml = ApplyPlaceholders(styledHtml, request.PlaceholderValues);
            }

            return styledHtml;
        }

        public async Task<UploadImageResponseDto> UploadTemplateImageAsync(IFormFile file)
        {
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                    return new UploadImageResponseDto { Success = false, ErrorMessage = "File is empty" };

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return new UploadImageResponseDto { Success = false, ErrorMessage = "Invalid file type. Allowed: JPG, JPEG, PNG, GIF, BMP, WEBP" };

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                    return new UploadImageResponseDto { Success = false, ErrorMessage = "File size too large. Maximum 5MB allowed." };

                // Create uploads directory
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var uploadsPath = Path.Combine(baseDirectory, "Templates", "template-designer.html");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return URL
                var fileUrl = $"/uploads/templates/{fileName}";

                return new UploadImageResponseDto
                {
                    Success = true,
                    FileName = file.FileName,
                    Url = fileUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload template image");
                return new UploadImageResponseDto { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<FontOptionsDto> GetFontOptionsAsync()
        {
            return await Task.FromResult(new FontOptionsDto());
        }

        public async Task<string> ConvertToPlainTextAsync(string htmlContent)
        {
            // Simple HTML to plain text conversion
            var plainText = htmlContent
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .Replace("<p>", "")
                .Replace("</p>", "\n\n")
                .Replace("<div>", "")
                .Replace("</div>", "\n")
                .Replace("&nbsp;", " ");

            // Remove all HTML tags
            plainText = System.Text.RegularExpressions.Regex.Replace(plainText, "<.*?>", string.Empty);

            // Decode HTML entities
            plainText = System.Net.WebUtility.HtmlDecode(plainText);

            return await Task.FromResult(plainText.Trim());
        }

        private string ApplyDesignStyles(string htmlContent, TemplateDesignOptions options)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: {options.FontFamily};
            font-size: {options.FontSize};
            color: {options.TextColor};
            background-color: {options.BackgroundColor};
            margin: 0;
            padding: {options.Padding};
            line-height: {options.LineHeight};
        }}
        .email-container {{
            max-width: 600px;
            margin: {options.Margin} auto;
            background: white;
            border: 1px solid {options.BorderColor};
            border-radius: {options.BorderRadius};
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        .email-header {{
            background: {options.HeaderBackground};
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .email-body {{
            padding: 30px;
            background: white;
        }}
        .email-footer {{
            background: {options.FooterBackground};
            padding: 20px;
            text-align: center;
            color: {options.TextColor};
            font-size: 12px;
            border-top: 1px solid {options.BorderColor};
        }}
        .button {{
            background-color: {options.ButtonColor};
            color: {options.ButtonTextColor};
            padding: 12px 24px;
            text-decoration: none;
            border-radius: {options.BorderRadius};
            display: inline-block;
            margin: 10px 0;
            border: none;
            cursor: pointer;
        }}
        h1, h2, h3, h4, h5, h6 {{
            color: {options.HeadingColor};
            margin-top: 0;
        }}
        a {{
            color: {options.LinkColor};
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        .text-left {{ text-align: left; }}
        .text-center {{ text-align: center; }}
        .text-right {{ text-align: right; }}
        .text-justify {{ text-align: justify; }}
        .bold {{ font-weight: bold; }}
        .italic {{ font-style: italic; }}
        .underline {{ text-decoration: underline; }}
        .strikethrough {{ text-decoration: line-through; }}
        .highlight {{ background-color: yellow; }}
        .image-container {{ 
            margin: 10px 0; 
            text-align: center;
        }}
        .image-container img {{
            max-width: 100%;
            height: auto;
            border-radius: 4px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 10px 0;
        }}
        table, th, td {{
            border: 1px solid {options.BorderColor};
        }}
        th, td {{
            padding: 8px 12px;
            text-align: left;
        }}
        th {{
            background-color: {options.FooterBackground};
        }}
        blockquote {{
            border-left: 4px solid {options.PrimaryColor};
            margin: 10px 0;
            padding-left: 15px;
            font-style: italic;
            color: {options.SecondaryColor};
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        {htmlContent}
    </div>
</body>
</html>";
        }

        private static string ApplyPlaceholders(string htmlContent, Dictionary<string, string> placeholders)
        {
            var processedContent = htmlContent;

            foreach (var placeholder in placeholders)
            {
                processedContent = processedContent.Replace(
                    $"{{{placeholder.Key}}}",
                    placeholder.Value ?? string.Empty
                );
            }

            // Apply default placeholders
            processedContent = processedContent
                .Replace("{CurrentDate}", DateTime.Now.ToString("MMMM dd, yyyy"))
                .Replace("{CurrentYear}", DateTime.Now.Year.ToString());

            return processedContent;
        }

    }
}
