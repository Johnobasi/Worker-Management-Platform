namespace WorkersManagement.Domain.Dtos
{
    public class UpdateTeamDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public int DepartmentCount { get; set; }
    }
}
