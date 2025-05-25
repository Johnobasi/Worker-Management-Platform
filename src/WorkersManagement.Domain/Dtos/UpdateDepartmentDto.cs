namespace WorkersManagement.Domain.Dtos
{
    public class UpdateDepartmentDto
    {
        public Guid Id { get; set; }                // Required for matching
        public string Name { get; set; }
        public string Description { get; set; }
        public string TeamName { get; set; }        // Optional: change associated team by name
    }

}
