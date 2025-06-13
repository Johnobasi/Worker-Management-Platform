using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos
{
    public class CreateDepartmenDto
    {
        [Required(ErrorMessage = "Department name description is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Team name description is required.")]
        public string TeamName { get; set; }
        [Required(ErrorMessage = "Sub Team description is required.")]
        public string SubTeamName { get; set; }
    }
}
