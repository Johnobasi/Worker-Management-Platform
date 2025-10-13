using Microsoft.EntityFrameworkCore;
using WorkersManagement.Core.DTOS.EmailComposerDtos;
using WorkersManagement.Domain.Interfaces;
using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Core.Repositories
{
    public class TemplateDesignerService : ITemplateDesignerService
    {
        private readonly WorkerDbContext _context;
        public TemplateDesignerService(WorkerDbContext context)
        {
            _context = context;
        }

        public Task<string> ApplyPlaceholdersAsync(string htmlContent, Dictionary<string, string> placeholders)
        {
            throw new NotImplementedException();
        }

        public Task<TemplateDesignResponseDto> CreateTemplateAsync(TemplateDesignRequestDto request)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteTemplateAsync(Guid templateId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GeneratePreviewAsync(TemplatePreviewRequestDto request)
        {
            throw new NotImplementedException();
        }

        public Task<List<TemplateDesignResponseDto>> GetAllTemplatesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<TemplateDesignResponseDto> GetTemplateAsync(Guid templateId)
        {
            throw new NotImplementedException();
        }

        public Task<TemplateDesignResponseDto> UpdateTemplateAsync(Guid templateId, TemplateDesignRequestDto request)
        {
            throw new NotImplementedException();
        }

        public async Task<List<EmailTemplateDto>> GetAllEmailTemplatesAsync()
        {
            return await _context.EmailTemplates
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => MapToDto(t))
                .ToListAsync();
        }

        public async Task<EmailTemplateDto> GetEmailTemplateAsync(string templateName)
        {
            var template = await _context.EmailTemplates
              .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive);

            return template != null ? MapToDto(template) : null;
        }

        public async Task<EmailTemplateDto> SaveEmailTemplateAsync(EmailTemplateDto templateDto)
        {
            var template = templateDto.Id == Guid.Empty ?
                new EmailTemplate() :
                await _context.EmailTemplates.FindAsync(templateDto.Id);

            if (template == null && templateDto.Id != Guid.Empty)
                throw new ArgumentException("Template not found");

            template ??= new EmailTemplate();

            template.Name = templateDto.Name;
            template.Subject = templateDto.Subject;
            template.HtmlContent = templateDto.Body;
            template.TemplateType = templateDto.TemplateType;
            template.UpdatedAt = DateTime.UtcNow;

            if (templateDto.Id == Guid.Empty)
            {
                template.CreatedAt = DateTime.UtcNow;
                template.IsActive = true;
                await _context.EmailTemplates.AddAsync(template);
            }
            else
            {
                _context.EmailTemplates.Update(template);
            }

            await _context.SaveChangesAsync();
            return MapToDto(template);
        }

        #region
        private static EmailTemplateDto MapToDto(EmailTemplate template)
        {
            return new EmailTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Subject = template.Subject,
                Body = template.HtmlContent,
                TemplateType = template.TemplateType,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                IsActive = template.IsActive
            };
        }
        #endregion

    }
}
