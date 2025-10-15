using Microsoft.AspNetCore.Http;
using WorkersManagement.Infrastructure.EmailComposerDtos;

namespace WorkersManagement.Domain.Interfaces
{
    public interface ITemplateDesignerService
    {
        Task<RichTextTemplateResponseDto> CreateTemplateAsync(RichTextTemplateRequestDto request);
        Task<RichTextTemplateResponseDto> UpdateTemplateAsync(Guid templateId, RichTextTemplateRequestDto request);
        Task<List<RichTextTemplateResponseDto>> GetAllTemplatesAsync();
        Task<RichTextTemplateResponseDto> GetTemplateAsync(Guid templateId);
        Task<bool> DeleteTemplateAsync(Guid templateId);
        string GeneratePreview(TemplatePreviewRequestDto request);
        Task<UploadImageResponseDto> UploadTemplateImageAsync(IFormFile file);
        Task<FontOptionsDto> GetFontOptionsAsync();
        Task<string> ConvertToPlainTextAsync(string htmlContent);
    }
}
