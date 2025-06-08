using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Infrastructure
{
    public class Team
    {
        public Guid Id { get;  set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<SubTeam> Subteams { get; set; }
        public ICollection<Department> Departments { get;  set; }
    }

}
