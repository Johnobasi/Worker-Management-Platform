using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using WorkersManagement.Infrastructure.EmailComposerDtos;

namespace WorkersManagement.Infrastructure.Entities
{
    public class EmailTemplate
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string HtmlContent { get; set; } = string.Empty;

        public string PlainTextContent { get; set; } = string.Empty;

        public string DesignOptionsJson { get; set; } = string.Empty;

        public string ImagesJson { get; set; } = string.Empty;

        public string TemplateType { get; set; } = "RichText";

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsOneTimeTemplate { get; set; } = false;

        public TemplateDesignOptions GetDesignOptions()
        {
            return string.IsNullOrEmpty(DesignOptionsJson)
                ? new TemplateDesignOptions()
                : JsonSerializer.Deserialize<TemplateDesignOptions>(DesignOptionsJson) ?? new TemplateDesignOptions();
        }

        public void SetDesignOptions(TemplateDesignOptions options)
        {
            DesignOptionsJson = JsonSerializer.Serialize(options);
        }

        public List<TemplateImageDto> GetImages()
        {
            return string.IsNullOrEmpty(ImagesJson)
                ? new List<TemplateImageDto>()
                : JsonSerializer.Deserialize<List<TemplateImageDto>>(ImagesJson) ?? new List<TemplateImageDto>();
        }

        public void SetImages(List<TemplateImageDto> images)
        {
            ImagesJson = JsonSerializer.Serialize(images);
        }
    }
}

