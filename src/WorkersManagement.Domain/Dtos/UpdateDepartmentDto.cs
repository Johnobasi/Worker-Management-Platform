using System.ComponentModel.DataAnnotations;

namespace WorkersManagement.Domain.Dtos
{
    public class UpdateDepartmentDto
    {
        [Required(ErrorMessage = "Department ID is required.")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Department name is required.")]// Required for matching
        public string Name { get; set; }

        [Required(ErrorMessage = "Team description is required.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Team name is required.")]
        public string TeamName { get; set; }        // Optional: change associated team by name
    }

}
