using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos
{
    public class UploadDevotionalRequest
    {
        [Required(ErrorMessage = "Please upload a file.")]
        [DataType(DataType.Upload)]
        [Display(Name = "Devotional File")]
        // Removed the invalid MaxFileSize attribute for file extensions
        [MaxFileSize(250 * 1024 * 1024, ErrorMessage = "File size must not exceed 250 MB.")]
        public IFormFile File { get; set; }
    }

    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly long _maxSizeInBytes;

        public MaxFileSizeAttribute(long maxSizeInBytes)
        {
            _maxSizeInBytes = maxSizeInBytes;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                if (file.Length > _maxSizeInBytes)
                {
                    return new ValidationResult(ErrorMessage ?? $"File size must not exceed {_maxSizeInBytes} bytes.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
