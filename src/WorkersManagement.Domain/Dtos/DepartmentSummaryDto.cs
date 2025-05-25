namespace WorkersManagement.Domain.Dtos
{
    public class DepartmentSummaryDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string TeamName { get; set; }
        public List<string> Users { get; set; } = new();
    }

}
