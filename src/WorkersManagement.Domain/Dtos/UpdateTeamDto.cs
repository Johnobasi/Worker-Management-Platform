using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos
{
    public class UpdateTeamDto
    {
        [Required(ErrorMessage = "Team ID is required.")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Team name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Team description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Department count is required.")]
        public int DepartmentCount { get; set; }
    }
}
