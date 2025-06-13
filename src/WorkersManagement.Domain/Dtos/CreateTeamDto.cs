using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos
{
    public class CreateTeamDto
    {
        [Required(ErrorMessage = "Team name is required.")]
        public string Name { get; set; } // Name of the team

        [Required(ErrorMessage = "Team description is required.")]
        public string Description { get; set; }
    }
}
