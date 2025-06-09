namespace WorkersManagement.Domain.Dtos
{
    public class AllDepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TeamName { get; set; }
        public string SubTeamName { get; set; }

        public List<string> Workers { get; set; } = new();
    }

}
