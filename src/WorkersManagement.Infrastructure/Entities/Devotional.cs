namespace WorkersManagement.Infrastructure.Entities
{
    public class Devotional
    {
        public Guid Id { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
