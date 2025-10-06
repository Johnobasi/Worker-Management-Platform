using WorkersManagement.Infrastructure;

namespace WorkersManagement.Core.DTOS
{
    public record WorkersSummaryResponse(
        Guid Id,
        string FullName,
        string Email,
        string DepartmentName,
        string QRCode,
        string? ProfilePictureUrl
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
                worker.ProfilePictureUrl
            );
        }

        public static IEnumerable<WorkersSummaryResponse> ToSummaryDtos(this IEnumerable<Worker> workers)
        {
            return workers.Select(w => w.ToSummaryDto());
        }
    }
}
