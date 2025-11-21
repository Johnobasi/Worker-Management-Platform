using WorkersManagement.Domain.Dtos.Habits;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Domain.Dtos.Workers
{
    public class UpdateWorkersDto
    {
        public ICollection<WorkerType> WorkerType { get; set; } = [];
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DepartmentName { get; set; }

        public ICollection<WorkerHabitPreferenceDto> HabitPreferences { get; set; }  = [];
    }


}
