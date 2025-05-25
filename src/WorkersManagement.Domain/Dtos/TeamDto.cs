namespace WorkersManagement.Domain.Dtos
{
    public class TeamDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> DepartmentNames { get; set; }
    }
}
