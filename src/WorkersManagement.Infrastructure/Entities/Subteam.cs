namespace WorkersManagement.Infrastructure.Entities
{
    public class SubTeam
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }           // The parent team
        public string Name { get; set; }
        public string Description { get; set; }
        public Team Team { get; set; }
        public ICollection<Department> Departments { get; set; }
    }
}
