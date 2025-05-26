namespace WorkersManagement.Infrastructure.Entities
{
    public class Department
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Team Teams { get; set; }
        public ICollection<Worker>  Workers { get; set; }
    }
}
