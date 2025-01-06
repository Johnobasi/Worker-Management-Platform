using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos
{
    public class CreateNewUserDto
    {
        public string Email { get; set; }
        public UserRole Role { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Guid? DepartmentId { get; set; }
    }
}
