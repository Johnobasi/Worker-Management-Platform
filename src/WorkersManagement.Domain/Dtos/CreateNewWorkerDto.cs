using WorkersManagement.Infrastructure.Enumerations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WorkersManagement.Domain.Dtos
{
    public class CreateNewWorkerDto
    {
        public string Email { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public UserRole Role { get; set; }
        public string Password { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DepartmentName { get; set; } 
    }
}
