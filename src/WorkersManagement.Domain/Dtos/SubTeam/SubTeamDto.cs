namespace WorkersManagement.Domain.Dtos.SubTeam
{    public class SubTeamDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreateSubTeamDto
    {
        public string Name { get; set; }
        public string TeamName { get; set; }
        public string Description { get; set; }
    }
}
