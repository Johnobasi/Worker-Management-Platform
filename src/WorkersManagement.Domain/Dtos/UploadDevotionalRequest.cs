using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos
{
    public class UploadDevotionalRequest
    {
        [Required]
        [DataType(DataType.Upload)]
        [Display(Name = "Devotional File")]
        [FileExtensions(Extensions = "pdf,docx,txt", ErrorMessage = "Please upload a valid file format (pdf, docx, txt).")]
        [MaxLength(10 * 1024 * 1024, ErrorMessage = "File size must not exceed 10 MB.")]

        public IFormFile File { get; set; }
    }
}
