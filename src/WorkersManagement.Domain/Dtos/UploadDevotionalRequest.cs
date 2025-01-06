using Microsoft.AspNetCore.Http;

namespace WorkersManagement.Domain.Dtos
{
    public class UploadDevotionalRequest
    {
        public IFormFile File { get; set; }
    }
}
