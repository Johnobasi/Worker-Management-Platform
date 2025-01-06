using WorkersManagement.Infrastructure.Entities;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Infrastructure
{
    public class User
    {
        public Guid Id { get;  set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get;  set; }
        public UserRole Role { get;  set; }
        public DateTime CreatedAt { get;  set; }
        public DateTime? LastLoginAt { get;  set; }
        public Guid? DepartmentId { get; set; }
        public Department Department { get; set; }

    }
}
