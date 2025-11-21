using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos
{
    public class CreateNewWorkerDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Department name is required.")]
        public string DepartmentName { get; set; }

        [Required(ErrorMessage = "Worker type is required.")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ICollection<WorkerType> WorkerType { get; set; }

        public IFormFile ProfilePicture { get; set; } = null;

        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public List<HabitType> HabitPreferences { get; set; } = new();
    }
}
