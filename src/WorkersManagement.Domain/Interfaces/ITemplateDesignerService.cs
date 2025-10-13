using WorkersManagement.Core.DTOS.EmailComposerDtos;

namespace WorkersManagement.Domain.Interfaces
{
    public interface ITemplateDesignerService
    {
        Task<TemplateDesignResponseDto> CreateTemplateAsync(TemplateDesignRequestDto request);
        Task<TemplateDesignResponseDto> UpdateTemplateAsync(Guid templateId, TemplateDesignRequestDto request);
        Task<List<TemplateDesignResponseDto>> GetAllTemplatesAsync();
        Task<TemplateDesignResponseDto> GetTemplateAsync(Guid templateId);
        Task<bool> DeleteTemplateAsync(Guid templateId);
        Task<string> GeneratePreviewAsync(TemplatePreviewRequestDto request);
        Task<string> ApplyPlaceholdersAsync(string htmlContent, Dictionary<string, string> placeholders);
        Task<EmailTemplateDto> GetEmailTemplateAsync(string templateName);
        Task<EmailTemplateDto> SaveEmailTemplateAsync(EmailTemplateDto templateDto);
        Task<List<EmailTemplateDto>> GetAllEmailTemplatesAsync();
    }
}
