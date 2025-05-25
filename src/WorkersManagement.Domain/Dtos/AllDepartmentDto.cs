namespace WorkersManagement.Domain.Dtos
{
    public class AllDepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TeamName { get; set; }

        // You can customize user details here
        public List<string> Users { get; set; } = new();
    }

}
