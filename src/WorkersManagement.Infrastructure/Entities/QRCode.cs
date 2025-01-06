namespace WorkersManagement.Infrastructure.Entities
{
    public class QRCode
    {
        public QRCode() { } // Explicit parameterless constructor
        public Guid Id { get; set; }
        public Guid UserId { get; set; } // FK to User
        public string QRCodeData { get; set; } // Base64-encoded QR code
        public DateTime CreatedAt { get; set; }
        public bool IsDisabled { get; set; }
    }
}
