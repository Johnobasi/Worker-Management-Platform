namespace WorkersManagement.Infrastructure.Entities
{
    public class UserRegistrationToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
