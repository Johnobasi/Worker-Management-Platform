namespace WorkersManagement.Domain.Dtos.WorkerAuthentication
{
    public class SetPasswordDto
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
