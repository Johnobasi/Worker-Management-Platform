using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Infrastructure.Configurations
{
    public class QRCodeConfiguration : IEntityTypeConfiguration<QRCode>
    {
        public void Configure(EntityTypeBuilder<QRCode> builder)
        {
            builder.HasKey(q => q.Id);

            builder.Property(q => q.QRCodeData)
                  .IsRequired();

            builder.Property(q => q.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne<User>()
                  .WithOne() // Assuming one-to-one relationship with User
                  .HasForeignKey<QRCode>(q => q.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
