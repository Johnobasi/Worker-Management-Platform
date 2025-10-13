using Microsoft.AspNetCore.Http;

namespace WorkersManagement.Core.DTOS.EmailComposerDtos
{
    public class BulkEmailDto
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public List<string> RecipientEmails { get; set; } = new();
        public List<EmailAttachmentDto> Attachments { get; set; } = new();
    }

    public class EmailAttachmentDto
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
    }

    public class EmailTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string TemplateType { get; set; } = "Html";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SendEmailRequestDto
    {
        public Guid? TemplateId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<Guid> SelectedWorkerIds { get; set; } = new();
        public bool SendToAll { get; set; } = true;
        public List<IFormFile>? Attachments { get; set; }
    }

    // Template Designer DTOs
    public class TemplateDesignRequestDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public TemplateStyles Styles { get; set; } = new();
        public List<TemplatePlaceholder> Placeholders { get; set; } = new();
        public bool SaveAsTemplate { get; set; }
    }

    public class TemplateStyles
    {
        public string FontFamily { get; set; } = "Arial, sans-serif";
        public string FontSize { get; set; } = "14px";
        public string PrimaryColor { get; set; } = "#2563eb";
        public string SecondaryColor { get; set; } = "#6b7280";
        public string BackgroundColor { get; set; } = "#ffffff";
        public string HeaderColor { get; set; } = "#1f2937";
        public string ButtonColor { get; set; } = "#2563eb";
        public string ButtonTextColor { get; set; } = "#ffffff";
        public string BorderRadius { get; set; } = "8px";
    }

    public class TemplatePlaceholder
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
    }

    public class TemplateDesignResponseDto
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string PreviewHtml { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TemplatePreviewRequestDto
    {
        public string HtmlContent { get; set; } = string.Empty;
        public TemplateStyles Styles { get; set; } = new();
        public Dictionary<string, string> PlaceholderValues { get; set; } = new();
    }

    // Email Composer DTOs
    public class EmailComposerRequestDto
    {
        public Guid? TemplateId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public List<string> To { get; set; } = new();
        public string Body { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public bool SendToAllWorkers { get; set; }
        public List<Guid> SelectedWorkerIds { get; set; } = new();
        public List<IFormFile>? Attachments { get; set; }
    }

    public class EmailComposerResponseDto
    {
        public Guid ComposerId { get; set; }
        public string PreviewHtml { get; set; } = string.Empty;
        public int RecipientCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TemplateListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
