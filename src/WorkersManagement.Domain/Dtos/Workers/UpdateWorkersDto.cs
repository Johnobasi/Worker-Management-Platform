using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos.Workers
{
    public class UpdateWorkersDto
    {
        public UserRole Role { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DepartmentName { get; set; }
    }
}
