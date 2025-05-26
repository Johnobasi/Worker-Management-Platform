namespace WorkersManagement.Infrastructure.Entities
{
    public class QRCode
    {
        public Guid Id { get; set; }
        public Guid WorkerId { get; set; }
        public byte[] QRCodeImage { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

}
