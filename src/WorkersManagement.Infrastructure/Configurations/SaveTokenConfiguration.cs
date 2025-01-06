using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkersManagement.Infrastructure.Entities;

namespace WorkersManagement.Infrastructure.Configurations
{
    public class SaveTokenConfiguration : IEntityTypeConfiguration<UserRegistrationToken>
    {
        public void Configure(EntityTypeBuilder<UserRegistrationToken> builder)
        {
            builder.HasKey(urt => urt.Id);

            builder.HasOne<User>()
                .WithMany() 
                .HasForeignKey(urt => urt.UserId)
                .OnDelete(DeleteBehavior.Cascade); 

            builder.Property(urt => urt.Token)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(urt => urt.ExpiryDate)
                .IsRequired();

            builder.Property(urt => urt.IsUsed)
                .HasDefaultValue(false);
        }
    }
}
