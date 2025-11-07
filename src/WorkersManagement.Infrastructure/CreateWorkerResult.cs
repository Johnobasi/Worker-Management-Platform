namespace WorkersManagement.Infrastructure
{
    public class CreateWorkerResult
    {
        public Guid WorkerId { get; set; }
        public string WorkerNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public string JwtToken { get; set; } = string.Empty;
    }
}
