using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using WorkersManagement.Core.DTOS.EmailComposerDtos;

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

        public string StylesJson { get; set; } = string.Empty;

        public string PlaceholdersJson { get; set; } = string.Empty;

        public string TemplateType { get; set; } = "Custom";

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public TemplateStyles GetStyles()
        {
            return string.IsNullOrEmpty(StylesJson)
                ? new TemplateStyles()
                : JsonSerializer.Deserialize<TemplateStyles>(StylesJson) ?? new TemplateStyles();
        }

        public void SetStyles(TemplateStyles styles)
        {
            StylesJson = JsonSerializer.Serialize(styles);
        }

        public List<TemplatePlaceholder> GetPlaceholders()
        {
            return string.IsNullOrEmpty(PlaceholdersJson)
                ? new List<TemplatePlaceholder>()
                : JsonSerializer.Deserialize<List<TemplatePlaceholder>>(PlaceholdersJson) ?? new List<TemplatePlaceholder>();
        }

        public void SetPlaceholders(List<TemplatePlaceholder> placeholders)
        {
            PlaceholdersJson = JsonSerializer.Serialize(placeholders);
        }
    }
}
