using WorkersManagement.Infrastructure;

namespace WorkersManagement.Domain.Dtos
{
    public class AttendanceSummaryResponse
    {
        public string SummaryMessages { get; set; }
        public int TotalCount { get; set; }
    }
}
