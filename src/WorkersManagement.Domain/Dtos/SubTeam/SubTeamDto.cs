using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos.SubTeam
{    public class SubTeamDto
    {
        [Required(ErrorMessage = "Name is required.")]

        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }
    }

    public class CreateSubTeamDto
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string TeamName { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }
    }
}
