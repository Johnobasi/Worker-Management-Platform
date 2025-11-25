using WorkersManagement.Infrastructure;
using WorkersManagement.Infrastructure.Enumerations;

namespace WorkersManagement.Core.DTOS
{
   

    public record WorkersSummaryResponse(
        Guid Id,
        string FullName,
        string Email,
        string DepartmentName,
        string QRCode,
        string? ProfilePictureUrl,
        string WorkerNumber,
        string WorkerType,
        string HabitPreferences
    );

    public static class WorkerMapper
    {
        public static WorkersSummaryResponse ToSummaryDto(this Worker worker)
        {
            return new WorkersSummaryResponse(
                worker.Id,
                $"{worker.FirstName} {worker.LastName}".Trim(),
                worker.Email,
                worker.Department?.Name ?? "Unassigned",
                worker.QRCode,
                worker.ProfilePictureUrl,
                worker.WorkerNumber,
                worker.Type != null && worker.Type.Any()
                    ? string.Join(", ", worker.Type.Select(t => t.ToString()))
                    : "Unassigned",
                worker.HabitPreferences != null && worker.HabitPreferences.Any()
                    ? string.Join(", ", worker.HabitPreferences.Select(hp => hp.HabitType.ToString()))
                    : "No Preferences"
            );
        }

        public static IEnumerable<WorkersSummaryResponse> ToSummaryDtos(this IEnumerable<Worker> workers)
        {
            return workers.Select(w => w.ToSummaryDto());
        }
    }
}
