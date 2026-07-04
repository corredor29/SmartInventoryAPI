using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Users;
using Domain.ValueObject.Users.User;

namespace Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("user");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).HasColumnName("user_id");

            builder.Property(u => u.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired()
                .HasConversion(v => v.Value, v => new UserName(v));

            builder.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired()
                .HasConversion(v => v.Value, v => new Email(v));

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired()
                .HasConversion(v => v.Value, v => new PasswordHash(v));

            builder.Property(u => u.CreatedAt).HasColumnName("created_at");
            builder.Property(u => u.RoleId).HasColumnName("role_id");
            builder.Property(u => u.CustomerId).HasColumnName("customer_id");

            builder.HasIndex(u => u.Email).IsUnique();

            builder.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<User>(u => u.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
