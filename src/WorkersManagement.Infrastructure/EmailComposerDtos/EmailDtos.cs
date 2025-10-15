using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Infrastructure.EmailComposerDtos
{
    public class RichTextTemplateRequestDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string PlainTextContent { get; set; } = string.Empty;
        public TemplateDesignOptions DesignOptions { get; set; } = new();
        public bool SaveAsTemplate { get; set; } = true;
        public List<TemplateImageDto> Images { get; set; } = new();
    }

    public class BulkEmailDto
    {
        [Required]
        public List<string> RecipientEmails { get; set; } = new List<string>();

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public List<EmailAttachmentDto>? Attachments { get; set; }
    }

    public class EmailAttachmentDto
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }
    public class TemplateDesignOptions
    {
        public string FontFamily { get; set; } = "Arial, sans-serif";
        public string FontSize { get; set; } = "14px";
        public string PrimaryColor { get; set; } = "#2563eb";
        public string BackgroundColor { get; set; } = "#ffffff";
        public string TextColor { get; set; } = "#333333";
        public string HeadingColor { get; set; } = "#1f2937";
        public string LinkColor { get; set; } = "#2563eb";
        public string BorderColor { get; set; } = "#e5e7eb";
        public string ButtonColor { get; set; } = "#2563eb";
        public string ButtonTextColor { get; set; } = "#ffffff";
        public string BorderRadius { get; set; } = "8px";
        public string Padding { get; set; } = "20px";
        public string Margin { get; set; } = "10px";
        public string LineHeight { get; set; } = "1.6";
        public string HeaderBackground { get; set; } = "linear-gradient(135deg, #667eea 0%, #764ba2 100%)";
        public string FooterBackground { get; set; } = "#f8f9fa";
        public string SecondaryColor { get; set; } = "#ffffff";
    }

    public class TemplateImageDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Data { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    public class RichTextTemplateResponseDto
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string PreviewHtml { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public TemplateDesignOptions DesignOptions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class TemplatePreviewRequestDto
    {
        public string HtmlContent { get; set; } = string.Empty;
        public string PlainTextContent { get; set; } = string.Empty;
        public TemplateDesignOptions DesignOptions { get; set; } = new();
        public Dictionary<string, string> PlaceholderValues { get; set; } = new();
    }

    public class FontOptionsDto
    {
        public List<string> FontFamilies { get; set; } = new()
        {
            "Arial, sans-serif",
            "Helvetica, sans-serif",
            "Georgia, serif",
            "Times New Roman, serif",
            "Verdana, sans-serif",
            "Tahoma, sans-serif",
            "Trebuchet MS, sans-serif",
            "Courier New, monospace",
            "Brush Script MT, cursive",
            "Comic Sans MS, cursive"
        };

        public List<string> FontSizes { get; set; } = new()
        {
            "8px", "9px", "10px", "11px", "12px", "14px", "16px", "18px", "20px", "24px", "28px", "32px", "36px", "48px", "72px"
        };

        public List<string> HeadingSizes { get; set; } = new()
        {
            "h1", "h2", "h3", "h4", "h5", "h6"
        };
    }

    public class UploadImageResponseDto
    {
        public bool Success { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
